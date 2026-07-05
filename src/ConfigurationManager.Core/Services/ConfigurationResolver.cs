using ConfigurationManager.Core.Domain;
using ConfigurationManager.Core.Dtos;
using ConfigurationManager.Core.Enums;
using ConfigurationManager.Core.Interfaces;

namespace ConfigurationManager.Core.Services;

/// <summary>
/// Resolves effective configuration values by applying the layered override precedence:
///   Base (integration type default) &lt; Tenant &lt; Environment &lt; TenantEnvironment
/// (most specific wins). See docs/database-design.md for the full rationale.
///
/// Disabled-value behaviour (documented design decision): if the most specific matching
/// override is disabled, the resolver falls back to the next less-specific *enabled* layer
/// rather than failing outright. If no enabled override exists at any layer, it falls back to
/// the key's metadata DefaultValue (source layer KeyDefault). If that is also absent, the item
/// is returned with Value = null, SourceLayer = None, IsEnabled = false, and IsMissing = true
/// when the key is required - callers must check IsMissing/IsEnabled rather than assume a value.
/// </summary>
public class ConfigurationResolver : IConfigurationResolver
{
    public const string MaskedValue = "********";

    private readonly IConfigurationRepository _repository;
    private readonly ISecretProtector _secretProtector;

    public ConfigurationResolver(IConfigurationRepository repository, ISecretProtector secretProtector)
    {
        _repository = repository;
        _secretProtector = secretProtector;
    }

    public async Task<ConfigurationResolveResult> ResolveAsync(
        string tenantCode,
        string integrationName,
        string environmentName,
        bool revealSecrets = false,
        CancellationToken ct = default)
    {
        var tenant = await _repository.GetTenantByCodeAsync(tenantCode, ct);
        if (tenant is null || !tenant.IsEnabled)
        {
            return ConfigurationResolveResult.NotFound(ConfigurationResolveStatus.TenantNotFound);
        }

        var environment = await _repository.GetEnvironmentByNameAsync(environmentName, ct);
        if (environment is null || !environment.IsEnabled)
        {
            return ConfigurationResolveResult.NotFound(ConfigurationResolveStatus.EnvironmentNotFound);
        }

        var integration = await _repository.GetIntegrationByNameAsync(integrationName, ct);
        if (integration is null || !integration.IsEnabled)
        {
            return ConfigurationResolveResult.NotFound(ConfigurationResolveStatus.IntegrationNotFound);
        }

        var keys = await _repository.GetApplicableKeysAsync(integration.IntegrationTypeId, ct);
        var keyIds = keys.Select(k => k.Id).ToList();
        var values = await _repository.GetValuesAsync(keyIds, tenant.Id, environment.Id, ct);
        var valuesByKey = values.GroupBy(v => v.ConfigurationKeyId).ToDictionary(g => g.Key, g => g.ToList());

        var items = new List<ResolvedConfigurationItem>(keys.Count);

        foreach (var key in keys)
        {
            valuesByKey.TryGetValue(key.Id, out var candidates);
            items.Add(ResolveKey(key, tenant.Id, environment.Id, candidates ?? new List<ConfigurationValue>(), revealSecrets));
        }

        return new ConfigurationResolveResult
        {
            Status = ConfigurationResolveStatus.Ok,
            Items = items
        };
    }

    private ResolvedConfigurationItem ResolveKey(
        ConfigurationKey key,
        Guid tenantId,
        Guid environmentId,
        List<ConfigurationValue> candidates,
        bool revealSecrets)
    {
        // Precedence, most specific first: TenantEnvironment > Environment > Tenant > Base.
        var tenantEnvironment = candidates.FirstOrDefault(v => v.TenantId == tenantId && v.EnvironmentId == environmentId);
        var environmentOnly = candidates.FirstOrDefault(v => v.TenantId == null && v.EnvironmentId == environmentId);
        var tenantOnly = candidates.FirstOrDefault(v => v.TenantId == tenantId && v.EnvironmentId == null);
        var baseValue = candidates.FirstOrDefault(v => v.TenantId == null && v.EnvironmentId == null);

        (ConfigurationValue? winner, ConfigurationSourceLayer layer) = PickEnabled(tenantEnvironment, ConfigurationSourceLayer.TenantEnvironment)
            ?? PickEnabled(environmentOnly, ConfigurationSourceLayer.Environment)
            ?? PickEnabled(tenantOnly, ConfigurationSourceLayer.Tenant)
            ?? PickEnabled(baseValue, ConfigurationSourceLayer.Base)
            ?? (null, ConfigurationSourceLayer.None);

        string? resolvedValue;
        bool isEnabled;

        if (winner is not null)
        {
            resolvedValue = FormatValue(key, winner.Value, winner.EncryptedValue, revealSecrets);
            isEnabled = true;
        }
        else if (!string.IsNullOrEmpty(key.DefaultValue))
        {
            layer = ConfigurationSourceLayer.KeyDefault;
            resolvedValue = FormatValue(key, key.DefaultValue, encryptedValue: null, revealSecrets);
            isEnabled = true;
        }
        else
        {
            layer = ConfigurationSourceLayer.None;
            resolvedValue = null;
            isEnabled = false;
        }

        var isMissing = key.IsRequired && layer == ConfigurationSourceLayer.None;

        return new ResolvedConfigurationItem
        {
            ConfigurationKeyId = key.Id,
            KeyName = key.KeyName,
            DisplayName = key.DisplayName,
            DataType = key.DataType,
            Value = resolvedValue,
            SourceLayer = layer,
            IsSensitive = key.IsSensitive,
            IsEnabled = isEnabled,
            IsRequired = key.IsRequired,
            IsMissing = isMissing,
            Section = key.Section?.Name,
            Subsection = key.Subsection?.Name,
            Description = key.Description,
            HelpText = key.HelpText,
            VisibilityGroup = key.VisibilityGroup
        };
    }

    private static (ConfigurationValue?, ConfigurationSourceLayer)? PickEnabled(ConfigurationValue? candidate, ConfigurationSourceLayer layer)
    {
        if (candidate is null)
        {
            return null;
        }

        return candidate.IsEnabled ? (candidate, layer) : null;
    }

    private string? FormatValue(ConfigurationKey key, string? plainValue, string? encryptedValue, bool revealSecrets)
    {
        if (!key.IsSensitive)
        {
            return plainValue;
        }

        if (!revealSecrets)
        {
            return MaskedValue;
        }

        if (!string.IsNullOrEmpty(encryptedValue))
        {
            return _secretProtector.Unprotect(encryptedValue);
        }

        // KeyDefault fallback path for a sensitive key with no stored override: return the plain
        // metadata default as-is (it is not expected to hold a real secret - see ConfigurationKey.DefaultValue).
        return plainValue;
    }
}
