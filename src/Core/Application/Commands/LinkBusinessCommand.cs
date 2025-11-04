using GoogleConnectorService.Core.Domain;
using MediatR;

namespace GoogleConnectorService.Core.Application.Commands;

public class LinkBusinessCommand : IRequest<LinkBusinessResult>
{
    public Guid ProfileBusinessId { get; set; }
    public string GoogleLocationId { get; set; } = string.Empty; // Format: accounts/{accountId}/locations/{locationId}
    public string BusinessName { get; set; } = string.Empty;
    public string? Location { get; set; }
}

public class LinkBusinessResult
{
    public bool Success { get; set; }
    public GoogleBusiness? GoogleBusiness { get; set; }
    public string? ErrorMessage { get; set; }
}

public class LinkBusinessCommandHandler : IRequestHandler<LinkBusinessCommand, LinkBusinessResult>
{
    private readonly IGoogleBusinessRepository _businessRepository;

    public LinkBusinessCommandHandler(IGoogleBusinessRepository businessRepository)
    {
        _businessRepository = businessRepository;
    }

    public async Task<LinkBusinessResult> Handle(LinkBusinessCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Extract location ID from the full path if needed
            var locationId = request.GoogleLocationId;
            if (locationId.Contains("/locations/"))
            {
                var parts = locationId.Split(new[] { "/locations/" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    locationId = parts[1];
                }
            }

            // Check if GoogleBusiness already exists
            var existingBusiness = await _businessRepository.GetByIdAsync(locationId, cancellationToken);
            
            if (existingBusiness != null)
            {
                // Update existing business with profile link
                existingBusiness.ProfileBusinessId = request.ProfileBusinessId;
                existingBusiness.Name = request.BusinessName;
                if (!string.IsNullOrEmpty(request.Location))
                {
                    existingBusiness.Location = request.Location;
                }
                existingBusiness.UpdatedAt = DateTime.UtcNow;
                
                await _businessRepository.UpdateAsync(existingBusiness, cancellationToken);
                
                return new LinkBusinessResult
                {
                    Success = true,
                    GoogleBusiness = existingBusiness
                };
            }
            else
            {
                // Create new GoogleBusiness
                var googleBusiness = new GoogleBusiness
                {
                    BusinessId = locationId,
                    Name = request.BusinessName,
                    Location = request.Location ?? string.Empty,
                    ProfileBusinessId = request.ProfileBusinessId,
                    CreatedAt = DateTime.UtcNow
                };

                await _businessRepository.AddAsync(googleBusiness, cancellationToken);

                return new LinkBusinessResult
                {
                    Success = true,
                    GoogleBusiness = googleBusiness
                };
            }
        }
        catch (Exception ex)
        {
            return new LinkBusinessResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

