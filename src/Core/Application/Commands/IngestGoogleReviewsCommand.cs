using GoogleConnectorService.Core.Domain;
using MediatR;

namespace GoogleConnectorService.Core.Application.Commands;

public class IngestGoogleReviewsCommand : IRequest<IngestGoogleReviewsResult>
{
    public string BusinessId { get; set; } = string.Empty;
}

public class IngestGoogleReviewsResult
{
    public int ReviewsIngested { get; set; }
    public DateTime SyncDate { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class IngestGoogleReviewsCommandHandler : IRequestHandler<IngestGoogleReviewsCommand, IngestGoogleReviewsResult>
{
    private readonly IGoogleApiClient _googleApiClient;
    private readonly IGoogleReviewRepository _reviewRepository;
    private readonly IGoogleBusinessRepository _businessRepository;

    public IngestGoogleReviewsCommandHandler(
        IGoogleApiClient googleApiClient,
        IGoogleReviewRepository reviewRepository,
        IGoogleBusinessRepository businessRepository)
    {
        _googleApiClient = googleApiClient;
        _reviewRepository = reviewRepository;
        _businessRepository = businessRepository;
    }

    public async Task<IngestGoogleReviewsResult> Handle(IngestGoogleReviewsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Fetch reviews from Google API
            var reviews = await _googleApiClient.GetReviewsAsync(request.BusinessId, cancellationToken);

            // Store reviews in database
            foreach (var review in reviews)
            {
                await _reviewRepository.UpsertAsync(review, cancellationToken);
            }

            // Update last sync date
            var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
            if (business != null)
            {
                business.LastSyncDate = DateTime.UtcNow;
                business.UpdatedAt = DateTime.UtcNow;
                await _businessRepository.UpdateAsync(business, cancellationToken);
            }

            return new IngestGoogleReviewsResult
            {
                ReviewsIngested = reviews.Count,
                SyncDate = DateTime.UtcNow,
                Success = true
            };
        }
        catch (Exception ex)
        {
            return new IngestGoogleReviewsResult
            {
                ReviewsIngested = 0,
                SyncDate = DateTime.UtcNow,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}



