using GoogleConnectorService.Core.Domain;

namespace GoogleConnectorService.Core.Application;

public interface IGoogleBusinessRepository
{
    Task<GoogleBusiness?> GetByIdAsync(string businessId, CancellationToken cancellationToken = default);
    Task AddAsync(GoogleBusiness business, CancellationToken cancellationToken = default);
    Task UpdateAsync(GoogleBusiness business, CancellationToken cancellationToken = default);
}



