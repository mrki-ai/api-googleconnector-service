using GoogleConnectorService.Core.Application;
using GoogleConnectorService.Core.Domain;
using Microsoft.Azure.Cosmos;

namespace GoogleConnectorService.Infrastructure.Persistence;

public class GoogleReviewRepository : IGoogleReviewRepository
{
    private readonly Container _container;

    public GoogleReviewRepository(CosmosClient cosmosClient, string databaseName, string containerName)
    {
        _container = cosmosClient.GetContainer(databaseName, containerName);
    }

    public async Task<GoogleReview?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<GoogleReview>(
                id, 
                new PartitionKey(id), 
                cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<GoogleReview>> GetByBusinessIdAsync(string businessId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.BusinessId = @businessId ORDER BY c.ReviewDate DESC OFFSET @skip LIMIT @take")
            .WithParameter("@businessId", businessId)
            .WithParameter("@skip", skip)
            .WithParameter("@take", take);

        var iterator = _container.GetItemQueryIterator<GoogleReview>(query);
        var results = new List<GoogleReview>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        return results;
    }

    public async Task<int> GetCountByBusinessIdAsync(string businessId, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition(
            "SELECT VALUE COUNT(1) FROM c WHERE c.BusinessId = @businessId")
            .WithParameter("@businessId", businessId);

        var iterator = _container.GetItemQueryIterator<int>(query);
        
        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            return response.FirstOrDefault();
        }

        return 0;
    }

    public async Task UpsertAsync(GoogleReview review, CancellationToken cancellationToken = default)
    {
        await _container.UpsertItemAsync(review, new PartitionKey(review.Id), cancellationToken: cancellationToken);
    }
}



