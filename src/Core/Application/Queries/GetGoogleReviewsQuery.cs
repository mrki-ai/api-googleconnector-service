using GoogleConnectorService.Core.Domain;
using MediatR;

namespace GoogleConnectorService.Core.Application.Queries;

public class GetGoogleReviewsQuery : IRequest<GetGoogleReviewsResult>
{
    public string BusinessId { get; set; } = string.Empty;
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 100;
}

public class GetGoogleReviewsResult
{
    public List<GoogleReview> Reviews { get; set; } = new();
    public int TotalCount { get; set; }
}

public class GetGoogleReviewsQueryHandler : IRequestHandler<GetGoogleReviewsQuery, GetGoogleReviewsResult>
{
    private readonly IGoogleReviewRepository _reviewRepository;

    public GetGoogleReviewsQueryHandler(IGoogleReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<GetGoogleReviewsResult> Handle(GetGoogleReviewsQuery request, CancellationToken cancellationToken)
    {
        var reviews = await _reviewRepository.GetByBusinessIdAsync(
            request.BusinessId, 
            request.Skip, 
            request.Take, 
            cancellationToken);

        var totalCount = await _reviewRepository.GetCountByBusinessIdAsync(request.BusinessId, cancellationToken);

        return new GetGoogleReviewsResult
        {
            Reviews = reviews,
            TotalCount = totalCount
        };
    }
}



