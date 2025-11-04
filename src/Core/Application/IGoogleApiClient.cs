using GoogleConnectorService.Core.Domain;

namespace GoogleConnectorService.Core.Application;

public interface IGoogleApiClient
{
    Task<List<GoogleReview>> GetReviewsAsync(string businessId, CancellationToken cancellationToken = default);
}



