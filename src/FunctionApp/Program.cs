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

        // CosmosDB Client
        var cosmosConnectionString = configuration["CosmosDb:ConnectionString"] 
            ?? throw new InvalidOperationException("CosmosDb:ConnectionString is required");
        
        services.AddSingleton(sp =>
        {
            return new CosmosClient(cosmosConnectionString, new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            });
        });

        // Repositories
        var databaseName = configuration["CosmosDb:DatabaseName"] ?? "GoogleConnectorDb";
        var reviewsContainerName = configuration["CosmosDb:ReviewsContainerName"] ?? "Reviews";
        var businessesContainerName = configuration["CosmosDb:BusinessesContainerName"] ?? "Businesses";

        services.AddScoped<IGoogleReviewRepository>(sp =>
        {
            var cosmosClient = sp.GetRequiredService<CosmosClient>();
            return new GoogleReviewRepository(cosmosClient, databaseName, reviewsContainerName);
        });

        services.AddScoped<IGoogleBusinessRepository>(sp =>
        {
            var cosmosClient = sp.GetRequiredService<CosmosClient>();
            return new GoogleBusinessRepository(cosmosClient, databaseName, businessesContainerName);
        });

        // Google API Client
        services.AddScoped<IGoogleApiClient>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<GoogleApiClient>>();
            var credentialsJson = configuration["Google:CredentialsJson"] 
                ?? throw new InvalidOperationException("Google:CredentialsJson is required");
            return new GoogleApiClient(logger, credentialsJson);
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



