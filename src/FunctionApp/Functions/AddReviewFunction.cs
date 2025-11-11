using GoogleConnectorService.Core.Application;
using GoogleConnectorService.Core.Domain;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace GoogleConnectorService.FunctionApp.Functions;

public class AddReviewFunction
{
    private readonly IGoogleReviewRepository _reviewRepository;
    private readonly ILogger<AddReviewFunction> _logger;

    public AddReviewFunction(
        IGoogleReviewRepository reviewRepository,
        ILogger<AddReviewFunction> logger)
    {
        _reviewRepository = reviewRepository;
        _logger = logger;
    }

    [Function("AddReview")]
    public async Task<HttpResponseData> AddReview(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "reviews/add")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("AddReview function triggered");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync(cancellationToken);
            var reviewData = JsonSerializer.Deserialize<AddReviewRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (reviewData == null || string.IsNullOrEmpty(reviewData.BusinessId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "BusinessId is required" }, cancellationToken);
                return badResponse;
            }

            // Create review from request
            var review = new GoogleReview
            {
                Id = string.IsNullOrEmpty(reviewData.Id) ? Guid.NewGuid().ToString() : reviewData.Id,
                BusinessId = reviewData.BusinessId,
                ReviewerName = reviewData.ReviewerName ?? "Anonymous",
                Rating = reviewData.Rating,
                Comment = reviewData.Comment ?? string.Empty,
                ReviewDate = reviewData.ReviewDate ?? DateTime.UtcNow,
                Reply = reviewData.Reply,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Save review
            await _reviewRepository.UpsertAsync(review, cancellationToken);

            _logger.LogInformation("Review added successfully: {ReviewId} for business {BusinessId}", review.Id, review.BusinessId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                reviewId = review.Id,
                message = "Review added successfully"
            }, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding review");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message }, cancellationToken);
            return errorResponse;
        }
    }

    private class AddReviewRequest
    {
        public string? Id { get; set; }
        public string BusinessId { get; set; } = string.Empty;
        public string? ReviewerName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime? ReviewDate { get; set; }
        public string? Reply { get; set; }
    }
}


