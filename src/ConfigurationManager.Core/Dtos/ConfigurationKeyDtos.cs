using ConfigurationManager.Core.Enums;

namespace ConfigurationManager.Core.Dtos;

public sealed record ConfigurationKeyDto(
    Guid Id,
    string KeyName,
    string DisplayName,
    string? Description,
    ConfigurationDataType DataType,
    string? DefaultValue,
    bool IsRequired,
    bool IsSensitive,
    bool IsEnabled,
    VisibilityGroup VisibilityGroup,
    Guid? IntegrationTypeId,
    string? IntegrationTypeName,
    Guid? SectionId,
    string? SectionName,
    Guid? SubsectionId,
    string? SubsectionName,
    int SortOrder,
    string? ValidationRules,
    string? ExampleValue,
    string? HelpText,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy)
{
    /// <summary>Never contains a value - keys only carry metadata. DefaultValue above is informational only.</summary>
    public const string Note = "Configuration keys hold metadata only; see ConfigurationValueDto for stored values.";
}

public sealed class CreateConfigurationKeyRequest
{
    public required string KeyName { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public ConfigurationDataType DataType { get; init; } = ConfigurationDataType.String;
    public string? DefaultValue { get; init; }
    public bool IsRequired { get; init; }
    public bool IsSensitive { get; init; }
    public VisibilityGroup VisibilityGroup { get; init; } = VisibilityGroup.Admin;
    public Guid? IntegrationTypeId { get; init; }
    public Guid? SectionId { get; init; }
    public Guid? SubsectionId { get; init; }
    public int SortOrder { get; init; }
    public string? ValidationRules { get; init; }
    public string? ExampleValue { get; init; }
    public string? HelpText { get; init; }
}

public sealed class UpdateConfigurationKeyRequest
{
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public string? DefaultValue { get; init; }
    public bool IsRequired { get; init; }
    public bool IsSensitive { get; init; }
    public VisibilityGroup VisibilityGroup { get; init; }
    public Guid? SectionId { get; init; }
    public Guid? SubsectionId { get; init; }
    public int SortOrder { get; init; }
    public string? ValidationRules { get; init; }
    public string? ExampleValue { get; init; }
    public string? HelpText { get; init; }
}
