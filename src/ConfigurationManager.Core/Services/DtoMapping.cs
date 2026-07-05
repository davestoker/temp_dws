using ConfigurationManager.Core.Domain;
using ConfigurationManager.Core.Dtos;
using ConfigurationManager.Core.Enums;

namespace ConfigurationManager.Core.Services;

internal static class DtoMapping
{
    public static ConfigurationKeyDto ToDto(this ConfigurationKey key) => new(
        key.Id,
        key.KeyName,
        key.DisplayName,
        key.Description,
        key.DataType,
        key.DefaultValue,
        key.IsRequired,
        key.IsSensitive,
        key.IsEnabled,
        key.VisibilityGroup,
        key.IntegrationTypeId,
        key.IntegrationType?.Name,
        key.SectionId,
        key.Section?.Name,
        key.SubsectionId,
        key.Subsection?.Name,
        key.SortOrder,
        key.ValidationRules,
        key.ExampleValue,
        key.HelpText,
        key.CreatedAt,
        key.CreatedBy,
        key.UpdatedAt,
        key.UpdatedBy);

    public static ConfigurationSourceLayer LayerOf(Guid? tenantId, Guid? environmentId) => (tenantId, environmentId) switch
    {
        (not null, not null) => ConfigurationSourceLayer.TenantEnvironment,
        (null, not null) => ConfigurationSourceLayer.Environment,
        (not null, null) => ConfigurationSourceLayer.Tenant,
        (null, null) => ConfigurationSourceLayer.Base
    };

    public static ConfigurationValueDto ToDto(this ConfigurationValue value, bool isSensitive)
    {
        var displayValue = isSensitive
            ? (string.IsNullOrEmpty(value.EncryptedValue) ? null : ConfigurationResolver.MaskedValue)
            : value.Value;

        return new ConfigurationValueDto(
            value.Id,
            value.ConfigurationKeyId,
            value.ConfigurationKey?.KeyName ?? string.Empty,
            value.TenantId,
            value.Tenant?.TenantCode,
            value.EnvironmentId,
            value.Environment?.Name,
            LayerOf(value.TenantId, value.EnvironmentId),
            displayValue,
            isSensitive,
            value.IsEnabled,
            value.CreatedAt,
            value.CreatedBy,
            value.UpdatedAt,
            value.UpdatedBy);
    }
}
