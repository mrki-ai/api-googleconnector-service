using GoogleConnectorService.Core.Application;
using GoogleConnectorService.Core.Application.Queries;
using GoogleConnectorService.Core.Domain;
using Moq;
using Xunit;

namespace GoogleConnectorService.UnitTests.Queries;

public class GetGoogleReviewsQueryHandlerTests
{
    private readonly Mock<IGoogleReviewRepository> _mockReviewRepository;
    private readonly GetGoogleReviewsQueryHandler _handler;

    public GetGoogleReviewsQueryHandlerTests()
    {
        _mockReviewRepository = new Mock<IGoogleReviewRepository>();
        _handler = new GetGoogleReviewsQueryHandler(_mockReviewRepository.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnReviews_WhenDataExists()
    {
        // Arrange
        var businessId = "test-business-123";
        var reviews = new List<GoogleReview>
        {
            new GoogleReview
            {
                Id = "review-1",
                BusinessId = businessId,
                ReviewerName = "John Doe",
                Rating = 5,
                Comment = "Great service!",
                ReviewDate = DateTime.UtcNow
            }
        };

        _mockReviewRepository.Setup(x => x.GetByBusinessIdAsync(businessId, 0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);
        
        _mockReviewRepository.Setup(x => x.GetCountByBusinessIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetGoogleReviewsQuery { BusinessId = businessId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Reviews);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("review-1", result.Reviews[0].Id);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoDataExists()
    {
        // Arrange
        var businessId = "test-business-123";
        
        _mockReviewRepository.Setup(x => x.GetByBusinessIdAsync(businessId, 0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GoogleReview>());
        
        _mockReviewRepository.Setup(x => x.GetCountByBusinessIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetGoogleReviewsQuery { BusinessId = businessId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Reviews);
        Assert.Equal(0, result.TotalCount);
    }
}



