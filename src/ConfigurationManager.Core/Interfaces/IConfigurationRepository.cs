using ConfigurationManager.Core.Domain;

namespace ConfigurationManager.Core.Interfaces;

/// <summary>
/// Read access needed by <see cref="Services.ConfigurationResolver"/>. Kept separate from
/// EF Core so the resolver's precedence logic can be unit tested against any implementation
/// (a fake, or the real EF-backed repository in ConfigurationManager.Data).
/// </summary>
public interface IConfigurationRepository
{
    Task<Tenant?> GetTenantByCodeAsync(string tenantCode, CancellationToken ct = default);

    Task<EnvironmentDefinition?> GetEnvironmentByNameAsync(string environmentName, CancellationToken ct = default);

    Task<Integration?> GetIntegrationByNameAsync(string integrationName, CancellationToken ct = default);

    /// <summary>Enabled keys that apply to the given integration type: global keys (IntegrationTypeId == null) plus keys scoped to that type.</summary>
    Task<IReadOnlyList<ConfigurationKey>> GetApplicableKeysAsync(Guid integrationTypeId, CancellationToken ct = default);

    /// <summary>All value rows (any layer, enabled or not) for the given keys, tenant, and environment.</summary>
    Task<IReadOnlyList<ConfigurationValue>> GetValuesAsync(
        IReadOnlyCollection<Guid> configurationKeyIds,
        Guid tenantId,
        Guid environmentId,
        CancellationToken ct = default);
}
