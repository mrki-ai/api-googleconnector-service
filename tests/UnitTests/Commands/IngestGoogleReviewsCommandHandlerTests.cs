using GoogleConnectorService.Core.Application;
using GoogleConnectorService.Core.Application.Commands;
using GoogleConnectorService.Core.Domain;
using Moq;
using Xunit;

namespace GoogleConnectorService.UnitTests.Commands;

public class IngestGoogleReviewsCommandHandlerTests
{
    private readonly Mock<IGoogleApiClient> _mockGoogleApiClient;
    private readonly Mock<IGoogleReviewRepository> _mockReviewRepository;
    private readonly Mock<IGoogleBusinessRepository> _mockBusinessRepository;
    private readonly IngestGoogleReviewsCommandHandler _handler;

    public IngestGoogleReviewsCommandHandlerTests()
    {
        _mockGoogleApiClient = new Mock<IGoogleApiClient>();
        _mockReviewRepository = new Mock<IGoogleReviewRepository>();
        _mockBusinessRepository = new Mock<IGoogleBusinessRepository>();
        
        _handler = new IngestGoogleReviewsCommandHandler(
            _mockGoogleApiClient.Object,
            _mockReviewRepository.Object,
            _mockBusinessRepository.Object);
    }

    [Fact]
    public async Task Handle_ShouldIngestReviews_WhenApiReturnsData()
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
            },
            new GoogleReview
            {
                Id = "review-2",
                BusinessId = businessId,
                ReviewerName = "Jane Smith",
                Rating = 4,
                Comment = "Good experience",
                ReviewDate = DateTime.UtcNow
            }
        };

        var business = new GoogleBusiness
        {
            BusinessId = businessId,
            Name = "Test Business",
            Location = "Test Location"
        };

        _mockGoogleApiClient.Setup(x => x.GetReviewsAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);
        
        _mockBusinessRepository.Setup(x => x.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);

        var command = new IngestGoogleReviewsCommand { BusinessId = businessId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.ReviewsIngested);
        _mockReviewRepository.Verify(x => x.UpsertAsync(It.IsAny<GoogleReview>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockBusinessRepository.Verify(x => x.UpdateAsync(business, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenExceptionOccurs()
    {
        // Arrange
        var businessId = "test-business-123";
        _mockGoogleApiClient.Setup(x => x.GetReviewsAsync(businessId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API Error"));

        var command = new IngestGoogleReviewsCommand { BusinessId = businessId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(0, result.ReviewsIngested);
        Assert.NotNull(result.ErrorMessage);
    }
}



