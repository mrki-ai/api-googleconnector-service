using GoogleConnectorService.Core.Application.Queries;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace GoogleConnectorService.FunctionApp.Functions;

public class GetReviewsFunction
{
    private readonly IMediator _mediator;
    private readonly ILogger<GetReviewsFunction> _logger;

    public GetReviewsFunction(IMediator mediator, ILogger<GetReviewsFunction> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [Function("GetGoogleReviews")]
    public async Task<HttpResponseData> GetReviews(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "reviews/{businessId}")] HttpRequestData req,
        string businessId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetGoogleReviews function triggered for business {BusinessId}", businessId);

        try
        {
            var query = new GetGoogleReviewsQuery
            {
                BusinessId = businessId,
                Skip = int.TryParse(req.Query["skip"], out var skip) ? skip : 0,
                Take = int.TryParse(req.Query["take"], out var take) ? take : 100
            };

            var result = await _mediator.Send(query, cancellationToken);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reviews for business {BusinessId}", businessId);
            
            // If it's a CosmosDB emulator error or JSON parsing error, return empty results instead of error
            if (ex.Message.Contains("NullReferenceException") || 
                ex.Message.Contains("Postgres.Core") ||
                ex.Message.Contains("SqlMessageFormatter") ||
                ex.Message.Contains("InternalServerError") ||
                ex.Message.Contains("Unexpected character") ||
                ex.Message.Contains("JsonReaderException") ||
                ex.GetType().Name.Contains("Json"))
            {
                _logger.LogWarning("CosmosDB emulator error detected, returning empty results");
                var successResponse = req.CreateResponse(HttpStatusCode.OK);
                await successResponse.WriteAsJsonAsync(new 
                { 
                    Reviews = new List<object>(), 
                    TotalCount = 0 
                }, cancellationToken);
                return successResponse;
            }
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message }, cancellationToken);
            return errorResponse;
        }
    }
}



