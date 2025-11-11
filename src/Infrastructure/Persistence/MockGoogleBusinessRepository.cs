using GoogleConnectorService.Core.Application;
using GoogleConnectorService.Core.Domain;

namespace GoogleConnectorService.Infrastructure.Persistence;

public class MockGoogleBusinessRepository : IGoogleBusinessRepository
{
    private readonly Dictionary<string, GoogleBusiness> _businesses = new();
    private readonly Dictionary<Guid, GoogleBusiness> _businessesByProfileId = new();

    public MockGoogleBusinessRepository()
    {
        // Initialize with mock data
        var mockBusiness = new GoogleBusiness
        {
            BusinessId = "test-business-id",
            ProfileBusinessId = Guid.NewGuid(),
            Name = "Test Business",
            Location = "123 Test Street, Test City",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _businesses[mockBusiness.BusinessId] = mockBusiness;
        if (mockBusiness.ProfileBusinessId.HasValue)
        {
            _businessesByProfileId[mockBusiness.ProfileBusinessId.Value] = mockBusiness;
        }
    }

    public Task<GoogleBusiness?> GetByIdAsync(string businessId, CancellationToken cancellationToken = default)
    {
        _businesses.TryGetValue(businessId, out var business);
        return Task.FromResult<GoogleBusiness?>(business);
    }

    public Task<GoogleBusiness?> GetByProfileBusinessIdAsync(Guid profileBusinessId, CancellationToken cancellationToken = default)
    {
        _businessesByProfileId.TryGetValue(profileBusinessId, out var business);
        return Task.FromResult<GoogleBusiness?>(business);
    }

    public Task AddAsync(GoogleBusiness business, CancellationToken cancellationToken = default)
    {
        business.CreatedAt = DateTime.UtcNow;
        business.UpdatedAt = DateTime.UtcNow;
        
        _businesses[business.BusinessId] = business;
        if (business.ProfileBusinessId.HasValue)
        {
            _businessesByProfileId[business.ProfileBusinessId.Value] = business;
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(GoogleBusiness business, CancellationToken cancellationToken = default)
    {
        business.UpdatedAt = DateTime.UtcNow;
        
        _businesses[business.BusinessId] = business;
        if (business.ProfileBusinessId.HasValue)
        {
            _businessesByProfileId[business.ProfileBusinessId.Value] = business;
        }

        return Task.CompletedTask;
    }
}

