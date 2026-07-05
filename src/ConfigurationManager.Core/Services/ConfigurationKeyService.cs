using ConfigurationManager.Core.Domain;
using ConfigurationManager.Core.Dtos;
using ConfigurationManager.Core.Enums;
using ConfigurationManager.Core.Interfaces;

namespace ConfigurationManager.Core.Services;

/// <summary>Business logic for managing configuration key metadata (not values).</summary>
public class ConfigurationKeyService
{
    private readonly IConfigurationAdminRepository _repository;
    private readonly AuditLogger _auditLogger;

    public ConfigurationKeyService(IConfigurationAdminRepository repository, AuditLogger auditLogger)
    {
        _repository = repository;
        _auditLogger = auditLogger;
    }

    public async Task<IReadOnlyList<ConfigurationKeyDto>> ListAsync(Guid? integrationTypeId, Guid? sectionId, CancellationToken ct = default)
    {
        var keys = await _repository.ListKeysAsync(integrationTypeId, sectionId, ct);
        return keys.Select(k => k.ToDto()).ToList();
    }

    public async Task<ConfigurationKeyDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var key = await _repository.GetKeyByIdAsync(id, ct);
        return key?.ToDto();
    }

    public async Task<ConfigurationKeyDto> CreateAsync(CreateConfigurationKeyRequest request, string actor, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.KeyName))
        {
            throw new ConfigurationValidationException("KeyName is required.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new ConfigurationValidationException("DisplayName is required.");
        }

        var existing = await _repository.GetKeyByNameAsync(request.KeyName, ct);
        if (existing is not null)
        {
            throw new ConfigurationValidationException($"A configuration key named '{request.KeyName}' already exists.");
        }

        var isSensitive = request.IsSensitive || request.DataType == ConfigurationDataType.Secret;

        var now = DateTimeOffset.UtcNow;
        var key = new ConfigurationKey
        {
            Id = Guid.NewGuid(),
            KeyName = request.KeyName.Trim(),
            DisplayName = request.DisplayName.Trim(),
            Description = request.Description,
            DataType = request.DataType,
            DefaultValue = isSensitive ? null : request.DefaultValue,
            IsRequired = request.IsRequired,
            IsSensitive = isSensitive,
            IsEnabled = true,
            VisibilityGroup = request.VisibilityGroup,
            IntegrationTypeId = request.IntegrationTypeId,
            SectionId = request.SectionId,
            SubsectionId = request.SubsectionId,
            SortOrder = request.SortOrder,
            ValidationRules = request.ValidationRules,
            ExampleValue = request.ExampleValue,
            HelpText = request.HelpText,
            CreatedAt = now,
            CreatedBy = actor
        };

        var created = await _repository.AddKeyAsync(key, ct);
        await _auditLogger.LogAsync(nameof(ConfigurationKey), created.Id, "Created", actor,
            newValue: created.KeyName, ct: ct);

        return created.ToDto();
    }

    public async Task<ConfigurationKeyDto> UpdateAsync(Guid id, UpdateConfigurationKeyRequest request, string actor, CancellationToken ct = default)
    {
        var key = await _repository.GetKeyByIdAsync(id, ct)
            ?? throw new ConfigurationNotFoundException($"Configuration key '{id}' was not found.");

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new ConfigurationValidationException("DisplayName is required.");
        }

        key.DisplayName = request.DisplayName.Trim();
        key.Description = request.Description;
        key.DefaultValue = key.IsSensitive ? null : request.DefaultValue;
        key.IsRequired = request.IsRequired;
        key.IsSensitive = key.IsSensitive || request.IsSensitive; // sensitivity cannot be revoked once set, to avoid accidentally exposing an existing secret
        key.VisibilityGroup = request.VisibilityGroup;
        key.SectionId = request.SectionId;
        key.SubsectionId = request.SubsectionId;
        key.SortOrder = request.SortOrder;
        key.ValidationRules = request.ValidationRules;
        key.ExampleValue = request.ExampleValue;
        key.HelpText = request.HelpText;
        key.UpdatedAt = DateTimeOffset.UtcNow;
        key.UpdatedBy = actor;

        await _repository.UpdateKeyAsync(key, ct);
        await _auditLogger.LogAsync(nameof(ConfigurationKey), key.Id, "Updated", actor, ct: ct);

        return key.ToDto();
    }

    public async Task<ConfigurationKeyDto> SetEnabledAsync(Guid id, bool isEnabled, string actor, CancellationToken ct = default)
    {
        var key = await _repository.GetKeyByIdAsync(id, ct)
            ?? throw new ConfigurationNotFoundException($"Configuration key '{id}' was not found.");

        key.IsEnabled = isEnabled;
        key.UpdatedAt = DateTimeOffset.UtcNow;
        key.UpdatedBy = actor;

        await _repository.UpdateKeyAsync(key, ct);
        await _auditLogger.LogAsync(nameof(ConfigurationKey), key.Id, isEnabled ? "Enabled" : "Disabled", actor, ct: ct);

        return key.ToDto();
    }
}
