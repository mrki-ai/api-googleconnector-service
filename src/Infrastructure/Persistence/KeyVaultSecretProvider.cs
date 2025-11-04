using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace GoogleConnectorService.Infrastructure.Persistence;

public class KeyVaultSecretProvider
{
    private readonly SecretClient _secretClient;

    public KeyVaultSecretProvider(string keyVaultUri)
    {
        _secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
    }

    public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        var secret = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
        return secret.Value.Value;
    }
}



