using GoogleConnectorService.Core.Domain;

namespace GoogleConnectorService.Core.Application;

public interface IGoogleReviewRepository
{
    Task<GoogleReview?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<List<GoogleReview>> GetByBusinessIdAsync(string businessId, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> GetCountByBusinessIdAsync(string businessId, CancellationToken cancellationToken = default);
    Task UpsertAsync(GoogleReview review, CancellationToken cancellationToken = default);
}



