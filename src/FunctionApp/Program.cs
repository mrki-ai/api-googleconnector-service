using Azure.Identity;
using GoogleConnectorService.Core.Application;
using GoogleConnectorService.Infrastructure.Persistence;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // Application Insights
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // MediatR for CQRS
        services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssembly(typeof(IGoogleReviewRepository).Assembly));

        // CosmosDB Client - Use mock repositories if connection fails or USE_MOCK_STORAGE is set
        var useMockStorage = configuration["UseMockStorage"] == "true" || 
                             configuration["UseMockStorage"] == "True";
        var cosmosConnectionString = configuration["CosmosDb:ConnectionString"];

        // Force mock storage if explicitly set
        if (useMockStorage)
        {
            services.AddScoped<IGoogleReviewRepository>(sp => new MockGoogleReviewRepository());
            services.AddScoped<IGoogleBusinessRepository>(sp => new MockGoogleBusinessRepository());
        }
        else if (!string.IsNullOrEmpty(cosmosConnectionString))
        {
            try
            {
                var cosmosClientOptions = new CosmosClientOptions
                {
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                    }
                };
                
                // For local emulator, use Gateway mode (HTTP) and disable SSL validation if needed
                if (cosmosConnectionString.Contains("localhost:8081"))
                {
                    cosmosClientOptions.ConnectionMode = ConnectionMode.Gateway;
                    cosmosClientOptions.HttpClientFactory = () =>
                    {
                        var handler = new HttpClientHandler();
                        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                        return new HttpClient(handler);
                    };
                }
                
                var cosmosClient = new CosmosClient(cosmosConnectionString, cosmosClientOptions);
                
                services.AddSingleton(cosmosClient);

                // Repositories
                var databaseName = configuration["CosmosDb:DatabaseName"] ?? "GoogleConnectorDb";
                var reviewsContainerName = configuration["CosmosDb:ReviewsContainerName"] ?? "Reviews";
                var businessesContainerName = configuration["CosmosDb:BusinessesContainerName"] ?? "Businesses";

                // Initialize database and containers asynchronously
                Task.Run(async () =>
                {
                    try
                    {
                        var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
                        await database.Database.CreateContainerIfNotExistsAsync(
                            new ContainerProperties(reviewsContainerName, "/id"));
                        await database.Database.CreateContainerIfNotExistsAsync(
                            new ContainerProperties(businessesContainerName, "/businessId"));
                        Console.WriteLine($"✅ CosmosDB database '{databaseName}' and containers initialized");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️  Warning: Could not create CosmosDB containers: {ex.Message}");
                    }
                });

                services.AddScoped<IGoogleReviewRepository>(sp =>
                {
                    try
                    {
                        var cosmosClient = sp.GetRequiredService<CosmosClient>();
                        return new GoogleReviewRepository(cosmosClient, databaseName, reviewsContainerName);
                    }
                    catch
                    {
                        // Fallback to mock if CosmosDB connection fails
                        return new MockGoogleReviewRepository();
                    }
                });

                services.AddScoped<IGoogleBusinessRepository>(sp =>
                {
                    try
                    {
                        var cosmosClient = sp.GetRequiredService<CosmosClient>();
                        return new GoogleBusinessRepository(cosmosClient, databaseName, businessesContainerName);
                    }
                    catch
                    {
                        // Fallback to mock if CosmosDB connection fails
                        return new MockGoogleBusinessRepository();
                    }
                });
            }
            catch
            {
                // Fallback to mock repositories if CosmosDB setup fails
                useMockStorage = true;
            }
        }

        // Mock storage is already set above if useMockStorage was true
        if (!useMockStorage && string.IsNullOrEmpty(cosmosConnectionString))
        {
            // Fallback to mock if no connection string and not explicitly disabled
            services.AddScoped<IGoogleReviewRepository>(sp => new MockGoogleReviewRepository());
            services.AddScoped<IGoogleBusinessRepository>(sp => new MockGoogleBusinessRepository());
        }

        // HttpClient for Google API
        services.AddHttpClient();

        // Google API Client
        services.AddScoped<IGoogleApiClient>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<GoogleApiClient>>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            var credentialsJson = configuration["Google:CredentialsJson"] 
                ?? throw new InvalidOperationException("Google:CredentialsJson is required");
            return new GoogleApiClient(logger, credentialsJson, httpClient);
        });

        // Key Vault (optional)
        var keyVaultUri = configuration["KeyVault:Uri"];
        if (!string.IsNullOrEmpty(keyVaultUri))
        {
            services.AddSingleton(sp => new KeyVaultSecretProvider(keyVaultUri));
        }

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });
    })
    .Build();

host.Run();



