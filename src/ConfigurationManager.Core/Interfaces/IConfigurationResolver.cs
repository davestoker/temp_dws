using ConfigurationManager.Core.Dtos;

namespace ConfigurationManager.Core.Interfaces;

public interface IConfigurationResolver
{
    /// <summary>
    /// Resolves effective configuration for a tenant/integration/environment, applying the
    /// Base -> Tenant -> Environment -> TenantEnvironment override precedence and skipping
    /// disabled values by falling back to the next less-specific enabled layer.
    /// </summary>
    /// <param name="revealSecrets">
    /// When false (UI/listing mode, the default), sensitive values are masked as "********".
    /// When true (authorised runtime mode only), sensitive values are decrypted and returned in plain text.
    /// </param>
    Task<ConfigurationResolveResult> ResolveAsync(
        string tenantCode,
        string integrationName,
        string environmentName,
        bool revealSecrets = false,
        CancellationToken ct = default);
}
