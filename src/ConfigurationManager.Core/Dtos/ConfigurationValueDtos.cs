using ConfigurationManager.Core.Enums;

namespace ConfigurationManager.Core.Dtos;

/// <summary>
/// Safe (list/UI) representation of an override row. Value is always masked for sensitive
/// keys - this DTO is never used for the authorised runtime decrypt path.
/// </summary>
public sealed record ConfigurationValueDto(
    Guid Id,
    Guid ConfigurationKeyId,
    string KeyName,
    Guid? TenantId,
    string? TenantCode,
    Guid? EnvironmentId,
    string? EnvironmentName,
    ConfigurationSourceLayer Layer,
    string? Value,
    bool IsSensitive,
    bool IsEnabled,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);

/// <summary>
/// Creates a new override or updates the existing one for the same (key, tenant, environment)
/// combination. Leave TenantCode/EnvironmentName null to target the Base layer.
/// </summary>
public sealed class UpsertConfigurationValueRequest
{
    public required Guid ConfigurationKeyId { get; init; }
    public string? TenantCode { get; init; }
    public string? EnvironmentName { get; init; }

    /// <summary>Plain-text value supplied by the caller. For sensitive keys this is encrypted before storage and never echoed back.</summary>
    public string? Value { get; init; }

    public bool IsEnabled { get; init; } = true;
}
