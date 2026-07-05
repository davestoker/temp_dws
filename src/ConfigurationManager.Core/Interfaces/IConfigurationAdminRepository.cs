using ConfigurationManager.Core.Domain;

namespace ConfigurationManager.Core.Interfaces;

/// <summary>
/// Read/write data access needed by the management services. Separate from
/// <see cref="IConfigurationRepository"/> (the resolver's read-only view) so the runtime
/// resolve path and the management/write path can evolve independently.
/// </summary>
public interface IConfigurationAdminRepository
{
    // Reference data (list-only via the management API).
    Task<IReadOnlyList<Tenant>> ListTenantsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EnvironmentDefinition>> ListEnvironmentsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<IntegrationType>> ListIntegrationTypesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Integration>> ListIntegrationsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ConfigurationSection>> ListSectionsWithSubsectionsAsync(CancellationToken ct = default);

    Task<Tenant?> GetTenantByCodeAsync(string tenantCode, CancellationToken ct = default);
    Task<EnvironmentDefinition?> GetEnvironmentByNameAsync(string environmentName, CancellationToken ct = default);

    // Configuration keys.
    Task<IReadOnlyList<ConfigurationKey>> ListKeysAsync(Guid? integrationTypeId = null, Guid? sectionId = null, CancellationToken ct = default);
    Task<ConfigurationKey?> GetKeyByIdAsync(Guid id, CancellationToken ct = default);
    Task<ConfigurationKey?> GetKeyByNameAsync(string keyName, CancellationToken ct = default);
    Task<ConfigurationKey> AddKeyAsync(ConfigurationKey key, CancellationToken ct = default);
    Task UpdateKeyAsync(ConfigurationKey key, CancellationToken ct = default);

    // Configuration values / overrides.
    Task<IReadOnlyList<ConfigurationValue>> ListValuesAsync(Guid? configurationKeyId = null, Guid? tenantId = null, Guid? environmentId = null, CancellationToken ct = default);
    Task<ConfigurationValue?> GetValueByIdAsync(Guid id, CancellationToken ct = default);
    Task<ConfigurationValue?> FindValueAsync(Guid configurationKeyId, Guid? tenantId, Guid? environmentId, CancellationToken ct = default);
    Task<ConfigurationValue> AddValueAsync(ConfigurationValue value, CancellationToken ct = default);
    Task UpdateValueAsync(ConfigurationValue value, CancellationToken ct = default);

    // Audit log.
    Task AddAuditLogEntryAsync(AuditLogEntry entry, CancellationToken ct = default);
}
