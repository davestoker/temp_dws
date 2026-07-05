using System.Globalization;
using ConfigurationManager.Core.Domain;
using ConfigurationManager.Core.Dtos;
using ConfigurationManager.Core.Enums;
using ConfigurationManager.Core.Interfaces;

namespace ConfigurationManager.Core.Services;

/// <summary>Business logic for managing configuration value overrides (the four layers).</summary>
public class ConfigurationValueService
{
    private readonly IConfigurationAdminRepository _repository;
    private readonly ISecretProtector _secretProtector;
    private readonly AuditLogger _auditLogger;

    public ConfigurationValueService(IConfigurationAdminRepository repository, ISecretProtector secretProtector, AuditLogger auditLogger)
    {
        _repository = repository;
        _secretProtector = secretProtector;
        _auditLogger = auditLogger;
    }

    public async Task<IReadOnlyList<ConfigurationValueDto>> ListAsync(
        Guid? configurationKeyId, string? tenantCode, string? environmentName, CancellationToken ct = default)
    {
        Guid? tenantId = null;
        if (!string.IsNullOrWhiteSpace(tenantCode))
        {
            var tenant = await _repository.GetTenantByCodeAsync(tenantCode, ct)
                ?? throw new ConfigurationNotFoundException($"Tenant '{tenantCode}' was not found.");
            tenantId = tenant.Id;
        }

        Guid? environmentId = null;
        if (!string.IsNullOrWhiteSpace(environmentName))
        {
            var environment = await _repository.GetEnvironmentByNameAsync(environmentName, ct)
                ?? throw new ConfigurationNotFoundException($"Environment '{environmentName}' was not found.");
            environmentId = environment.Id;
        }

        var values = await _repository.ListValuesAsync(configurationKeyId, tenantId, environmentId, ct);
        return values.Select(v => v.ToDto(v.ConfigurationKey?.IsSensitive ?? false)).ToList();
    }

    public async Task<ConfigurationValueDto> UpsertAsync(UpsertConfigurationValueRequest request, string actor, CancellationToken ct = default)
    {
        var key = await _repository.GetKeyByIdAsync(request.ConfigurationKeyId, ct)
            ?? throw new ConfigurationNotFoundException($"Configuration key '{request.ConfigurationKeyId}' was not found.");

        Guid? tenantId = null;
        if (!string.IsNullOrWhiteSpace(request.TenantCode))
        {
            var tenant = await _repository.GetTenantByCodeAsync(request.TenantCode, ct)
                ?? throw new ConfigurationNotFoundException($"Tenant '{request.TenantCode}' was not found.");
            tenantId = tenant.Id;
        }

        Guid? environmentId = null;
        if (!string.IsNullOrWhiteSpace(request.EnvironmentName))
        {
            var environment = await _repository.GetEnvironmentByNameAsync(request.EnvironmentName, ct)
                ?? throw new ConfigurationNotFoundException($"Environment '{request.EnvironmentName}' was not found.");
            environmentId = environment.Id;
        }

        if (!key.IsSensitive && !string.IsNullOrEmpty(request.Value))
        {
            ValidateDataType(key, request.Value);
        }

        var existing = await _repository.FindValueAsync(key.Id, tenantId, environmentId, ct);
        var now = DateTimeOffset.UtcNow;
        bool secretReplaced = false;

        if (existing is null)
        {
            var newValue = new ConfigurationValue
            {
                Id = Guid.NewGuid(),
                ConfigurationKeyId = key.Id,
                TenantId = tenantId,
                EnvironmentId = environmentId,
                IsEnabled = request.IsEnabled,
                CreatedAt = now,
                CreatedBy = actor
            };

            ApplyValue(key, newValue, request.Value, ref secretReplaced);

            var created = await _repository.AddValueAsync(newValue, ct);
            await _auditLogger.LogAsync(nameof(ConfigurationValue), created.Id, "Created", actor,
                newValue: AuditLogger.Redacted(key.IsSensitive, request.Value),
                details: $"{key.KeyName} [{DtoMapping.LayerOf(tenantId, environmentId)}]", ct: ct);

            return created.ToDto(key.IsSensitive);
        }

        var oldDisplayValue = AuditLogger.Redacted(key.IsSensitive, existing.Value);
        ApplyValue(key, existing, request.Value, ref secretReplaced);
        existing.IsEnabled = request.IsEnabled;
        existing.UpdatedAt = now;
        existing.UpdatedBy = actor;

        await _repository.UpdateValueAsync(existing, ct);
        await _auditLogger.LogAsync(nameof(ConfigurationValue), existing.Id, secretReplaced ? "SecretReplaced" : "Updated", actor,
            oldValue: oldDisplayValue,
            newValue: AuditLogger.Redacted(key.IsSensitive, request.Value),
            details: $"{key.KeyName} [{DtoMapping.LayerOf(tenantId, environmentId)}]", ct: ct);

        return existing.ToDto(key.IsSensitive);
    }

    public async Task<ConfigurationValueDto> SetEnabledAsync(Guid id, bool isEnabled, string actor, CancellationToken ct = default)
    {
        var value = await _repository.GetValueByIdAsync(id, ct)
            ?? throw new ConfigurationNotFoundException($"Configuration value '{id}' was not found.");

        value.IsEnabled = isEnabled;
        value.UpdatedAt = DateTimeOffset.UtcNow;
        value.UpdatedBy = actor;

        await _repository.UpdateValueAsync(value, ct);

        var key = value.ConfigurationKey ?? await _repository.GetKeyByIdAsync(value.ConfigurationKeyId, ct);
        await _auditLogger.LogAsync(nameof(ConfigurationValue), value.Id, isEnabled ? "Enabled" : "Disabled", actor,
            details: key?.KeyName, ct: ct);

        return value.ToDto(key?.IsSensitive ?? false);
    }

    /// <summary>
    /// Sets the plain or encrypted value on the entity. When the key is sensitive and the
    /// caller did not supply a new value, the existing encrypted value is left untouched
    /// (this is how "enable/disable without replacing the secret" is supported).
    /// </summary>
    private void ApplyValue(ConfigurationKey key, ConfigurationValue target, string? requestedValue, ref bool secretReplaced)
    {
        if (!key.IsSensitive)
        {
            target.Value = requestedValue;
            target.EncryptedValue = null;
            return;
        }

        target.Value = null;

        if (!string.IsNullOrEmpty(requestedValue))
        {
            target.EncryptedValue = _secretProtector.Protect(requestedValue);
            secretReplaced = true;
        }
        // else: leave target.EncryptedValue as-is (unchanged secret).
    }

    private static void ValidateDataType(ConfigurationKey key, string value)
    {
        var valid = key.DataType switch
        {
            ConfigurationDataType.Int => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            ConfigurationDataType.Decimal => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _),
            ConfigurationDataType.Boolean => bool.TryParse(value, out _),
            ConfigurationDataType.DateTime => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            ConfigurationDataType.Json => IsValidJson(value),
            _ => true
        };

        if (!valid)
        {
            throw new ConfigurationValidationException($"Value '{value}' is not valid for data type {key.DataType} (key '{key.KeyName}').");
        }
    }

    private static bool IsValidJson(string value)
    {
        try
        {
            using var _ = System.Text.Json.JsonDocument.Parse(value);
            return true;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
