using GoogleConnectorService.Core.Application.Commands;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace GoogleConnectorService.FunctionApp.Functions;

public class LinkBusinessFunction
{
    private readonly IMediator _mediator;
    private readonly ILogger<LinkBusinessFunction> _logger;

    public LinkBusinessFunction(IMediator mediator, ILogger<LinkBusinessFunction> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [Function("LinkBusiness")]
    public async Task<HttpResponseData> LinkBusiness(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "businesses/link")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("LinkBusiness function triggered");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync(cancellationToken);
            var command = JsonSerializer.Deserialize<LinkBusinessCommand>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (command == null || 
                command.ProfileBusinessId == Guid.Empty || 
                string.IsNullOrEmpty(command.GoogleLocationId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "ProfileBusinessId and GoogleLocationId are required" }, cancellationToken);
                return badResponse;
            }

            var result = await _mediator.Send(command, cancellationToken);

            var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(result, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking business");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message }, cancellationToken);
            return errorResponse;
        }
    }
}

