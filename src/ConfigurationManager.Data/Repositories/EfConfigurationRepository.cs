using ConfigurationManager.Core.Domain;
using ConfigurationManager.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConfigurationManager.Data.Repositories;

/// <summary>
/// EF Core-backed implementation of both the resolver's read-only repository and the
/// management/admin read-write repository, sharing one DbContext. Split into two interfaces
/// at the Core layer so the resolver and the management services each depend only on what
/// they need.
/// </summary>
public class EfConfigurationRepository : IConfigurationRepository, IConfigurationAdminRepository
{
    private readonly ConfigurationDbContext _db;

    public EfConfigurationRepository(ConfigurationDbContext db)
    {
        _db = db;
    }

    // ---- IConfigurationRepository (resolver) ----

    public Task<Tenant?> GetTenantByCodeAsync(string tenantCode, CancellationToken ct = default) =>
        _db.Tenants.FirstOrDefaultAsync(t => t.TenantCode == tenantCode, ct);

    public Task<EnvironmentDefinition?> GetEnvironmentByNameAsync(string environmentName, CancellationToken ct = default) =>
        _db.Environments.FirstOrDefaultAsync(e => e.Name == environmentName, ct);

    public Task<Integration?> GetIntegrationByNameAsync(string integrationName, CancellationToken ct = default) =>
        _db.Integrations.Include(i => i.IntegrationType).FirstOrDefaultAsync(i => i.IntegrationName == integrationName, ct);

    public async Task<IReadOnlyList<ConfigurationKey>> GetApplicableKeysAsync(Guid integrationTypeId, CancellationToken ct = default) =>
        await _db.ConfigurationKeys
            .Include(k => k.Section)
            .Include(k => k.Subsection)
            .Where(k => k.IsEnabled && (k.IntegrationTypeId == null || k.IntegrationTypeId == integrationTypeId))
            .OrderBy(k => k.SortOrder)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ConfigurationValue>> GetValuesAsync(
        IReadOnlyCollection<Guid> configurationKeyIds, Guid tenantId, Guid environmentId, CancellationToken ct = default) =>
        await _db.ConfigurationValues
            .Where(v => configurationKeyIds.Contains(v.ConfigurationKeyId)
                && (v.TenantId == null || v.TenantId == tenantId)
                && (v.EnvironmentId == null || v.EnvironmentId == environmentId))
            .ToListAsync(ct);

    // ---- IConfigurationAdminRepository (management) ----

    public async Task<IReadOnlyList<Tenant>> ListTenantsAsync(CancellationToken ct = default) =>
        await _db.Tenants.OrderBy(t => t.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<EnvironmentDefinition>> ListEnvironmentsAsync(CancellationToken ct = default) =>
        await _db.Environments.OrderBy(e => e.SortOrder).ToListAsync(ct);

    public async Task<IReadOnlyList<IntegrationType>> ListIntegrationTypesAsync(CancellationToken ct = default) =>
        await _db.IntegrationTypes.OrderBy(i => i.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Integration>> ListIntegrationsAsync(CancellationToken ct = default) =>
        await _db.Integrations.Include(i => i.IntegrationType).OrderBy(i => i.IntegrationName).ToListAsync(ct);

    public async Task<IReadOnlyList<ConfigurationSection>> ListSectionsWithSubsectionsAsync(CancellationToken ct = default) =>
        await _db.ConfigurationSections.Include(s => s.Subsections).OrderBy(s => s.SortOrder).ToListAsync(ct);

    public async Task<IReadOnlyList<ConfigurationKey>> ListKeysAsync(Guid? integrationTypeId = null, Guid? sectionId = null, CancellationToken ct = default)
    {
        var query = _db.ConfigurationKeys
            .Include(k => k.IntegrationType)
            .Include(k => k.Section)
            .Include(k => k.Subsection)
            .AsQueryable();

        if (integrationTypeId.HasValue)
        {
            query = query.Where(k => k.IntegrationTypeId == integrationTypeId);
        }

        if (sectionId.HasValue)
        {
            query = query.Where(k => k.SectionId == sectionId);
        }

        return await query.OrderBy(k => k.SortOrder).ThenBy(k => k.DisplayName).ToListAsync(ct);
    }

    public Task<ConfigurationKey?> GetKeyByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.ConfigurationKeys.Include(k => k.IntegrationType).Include(k => k.Section).Include(k => k.Subsection)
            .FirstOrDefaultAsync(k => k.Id == id, ct);

    public Task<ConfigurationKey?> GetKeyByNameAsync(string keyName, CancellationToken ct = default) =>
        _db.ConfigurationKeys.FirstOrDefaultAsync(k => k.KeyName == keyName, ct);

    public async Task<ConfigurationKey> AddKeyAsync(ConfigurationKey key, CancellationToken ct = default)
    {
        _db.ConfigurationKeys.Add(key);
        await _db.SaveChangesAsync(ct);
        return key;
    }

    public Task UpdateKeyAsync(ConfigurationKey key, CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<ConfigurationValue>> ListValuesAsync(
        Guid? configurationKeyId = null, Guid? tenantId = null, Guid? environmentId = null, CancellationToken ct = default)
    {
        var query = _db.ConfigurationValues
            .Include(v => v.ConfigurationKey)
            .Include(v => v.Tenant)
            .Include(v => v.Environment)
            .AsQueryable();

        if (configurationKeyId.HasValue)
        {
            query = query.Where(v => v.ConfigurationKeyId == configurationKeyId);
        }

        if (tenantId.HasValue)
        {
            query = query.Where(v => v.TenantId == tenantId);
        }

        if (environmentId.HasValue)
        {
            query = query.Where(v => v.EnvironmentId == environmentId);
        }

        return await query.ToListAsync(ct);
    }

    public Task<ConfigurationValue?> GetValueByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.ConfigurationValues.Include(v => v.ConfigurationKey).FirstOrDefaultAsync(v => v.Id == id, ct);

    public Task<ConfigurationValue?> FindValueAsync(Guid configurationKeyId, Guid? tenantId, Guid? environmentId, CancellationToken ct = default) =>
        _db.ConfigurationValues.FirstOrDefaultAsync(v =>
            v.ConfigurationKeyId == configurationKeyId && v.TenantId == tenantId && v.EnvironmentId == environmentId, ct);

    public async Task<ConfigurationValue> AddValueAsync(ConfigurationValue value, CancellationToken ct = default)
    {
        _db.ConfigurationValues.Add(value);
        await _db.SaveChangesAsync(ct);
        return value;
    }

    public Task UpdateValueAsync(ConfigurationValue value, CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public async Task AddAuditLogEntryAsync(AuditLogEntry entry, CancellationToken ct = default)
    {
        _db.AuditLogEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
    }
}
