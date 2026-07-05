using ConfigurationManager.Core.Enums;

namespace ConfigurationManager.Core.Dtos;

/// <summary>
/// One resolved configuration key/value for a specific tenant + integration + environment.
/// In UI/safe mode, <see cref="Value"/> is masked for sensitive keys. In authorised runtime
/// mode, <see cref="Value"/> contains the decrypted plain text.
/// </summary>
public sealed class ResolvedConfigurationItem
{
    public required Guid ConfigurationKeyId { get; init; }

    public required string KeyName { get; init; }

    public required string DisplayName { get; init; }

    public ConfigurationDataType DataType { get; init; }

    /// <summary>Resolved value. Null when missing. Masked as "********" for sensitive keys unless the caller is authorised for decrypted runtime access.</summary>
    public string? Value { get; init; }

    public ConfigurationSourceLayer SourceLayer { get; init; }

    public bool IsSensitive { get; init; }

    /// <summary>Whether the resolved value (or the key itself) is currently enabled.</summary>
    public bool IsEnabled { get; init; }

    public bool IsRequired { get; init; }

    /// <summary>True when the key is required but no enabled value could be resolved at any layer.</summary>
    public bool IsMissing { get; init; }

    public string? Section { get; init; }

    public string? Subsection { get; init; }

    public string? Description { get; init; }

    public string? HelpText { get; init; }

    public VisibilityGroup VisibilityGroup { get; init; }
}
