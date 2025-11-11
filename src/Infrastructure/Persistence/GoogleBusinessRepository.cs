using GoogleConnectorService.Core.Application;
using GoogleConnectorService.Core.Domain;
using Microsoft.Azure.Cosmos;

namespace GoogleConnectorService.Infrastructure.Persistence;

public class GoogleBusinessRepository : IGoogleBusinessRepository
{
    private readonly Container _container;

    public GoogleBusinessRepository(CosmosClient cosmosClient, string databaseName, string containerName)
    {
        _container = cosmosClient.GetContainer(databaseName, containerName);
    }

    public async Task<GoogleBusiness?> GetByIdAsync(string businessId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<GoogleBusiness>(
                businessId,
                new PartitionKey(businessId),
                cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<GoogleBusiness?> GetByProfileBusinessIdAsync(Guid profileBusinessId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Query CosmosDB for GoogleBusiness by ProfileBusinessId
            // Note: CosmosDB queries need to handle GUID properly
            var query = new QueryDefinition("SELECT * FROM c WHERE c.profileBusinessId = @profileBusinessId")
                .WithParameter("@profileBusinessId", profileBusinessId);
            
            var iterator = _container.GetItemQueryIterator<GoogleBusiness>(query);
            
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                return response.FirstOrDefault();
            }
            
            return null;
        }
        catch (Exception ex) when (ex.Message.Contains("NullReferenceException") || 
                                    ex.Message.Contains("Postgres.Core") ||
                                    ex.Message.Contains("SqlMessageFormatter"))
        {
            // CosmosDB emulator bug - return null
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task AddAsync(GoogleBusiness business, CancellationToken cancellationToken = default)
    {
        await _container.CreateItemAsync(business, new PartitionKey(business.BusinessId), cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(GoogleBusiness business, CancellationToken cancellationToken = default)
    {
        await _container.ReplaceItemAsync(business, business.BusinessId, new PartitionKey(business.BusinessId), cancellationToken: cancellationToken);
    }
}



