using GoogleConnectorService.Core.Application.Commands;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace GoogleConnectorService.FunctionApp.Functions;

public class IngestReviewsFunction
{
    private readonly IMediator _mediator;
    private readonly ILogger<IngestReviewsFunction> _logger;

    public IngestReviewsFunction(IMediator mediator, ILogger<IngestReviewsFunction> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [Function("IngestGoogleReviews")]
    public async Task<HttpResponseData> IngestReviews(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "reviews/ingest")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("IngestGoogleReviews function triggered");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync(cancellationToken);
            var command = JsonSerializer.Deserialize<IngestGoogleReviewsCommand>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (command == null || string.IsNullOrEmpty(command.BusinessId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "BusinessId is required" }, cancellationToken);
                return badResponse;
            }

            var result = await _mediator.Send(command, cancellationToken);

            var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(result, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting reviews");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message }, cancellationToken);
            return errorResponse;
        }
    }

    [Function("IngestGoogleReviewsTimer")]
    public Task IngestReviewsTimer(
        [TimerTrigger("0 0 */6 * * *")] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scheduled ingestion triggered at {Time}", DateTime.UtcNow);

        // In a real scenario, you would fetch a list of all active businesses
        // and ingest reviews for each one
        // For POC, this is a placeholder
        
        _logger.LogInformation("Scheduled ingestion completed");
        return Task.CompletedTask;
    }
}



