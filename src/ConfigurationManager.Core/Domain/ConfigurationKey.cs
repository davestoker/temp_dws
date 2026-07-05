using ConfigurationManager.Core.Enums;

namespace ConfigurationManager.Core.Domain;

/// <summary>
/// Metadata describing a single configuration key. The actual values (base and overrides)
/// live in <see cref="ConfigurationValue"/> rows keyed by (TenantId, EnvironmentId).
/// </summary>
public class ConfigurationKey : IAuditable
{
    public Guid Id { get; set; }

    /// <summary>Unique, stable name, e.g. "EngagingNetworks.ClientSecret".</summary>
    public string KeyName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ConfigurationDataType DataType { get; set; } = ConfigurationDataType.String;

    /// <summary>
    /// Informational default shown in the UI when no ConfigurationValue row exists at all
    /// (source layer <see cref="ConfigurationSourceLayer.KeyDefault"/>). Never used for Secret
    /// keys - secrets must be set explicitly.
    /// </summary>
    public string? DefaultValue { get; set; }

    public bool IsRequired { get; set; }

    /// <summary>
    /// Whether the value is sensitive and must be encrypted at rest / masked in the UI.
    /// Always true when <see cref="DataType"/> is <see cref="ConfigurationDataType.Secret"/>.
    /// </summary>
    public bool IsSensitive { get; set; }

    /// <summary>Whether the key is active. Disabled keys are excluded entirely from resolution.</summary>
    public bool IsEnabled { get; set; } = true;

    public VisibilityGroup VisibilityGroup { get; set; } = VisibilityGroup.Admin;

    /// <summary>Null = global key, applies to any integration type. Non-null = scoped to one integration type (the "Base" layer owner).</summary>
    public Guid? IntegrationTypeId { get; set; }
    public IntegrationType? IntegrationType { get; set; }

    public Guid? SectionId { get; set; }
    public ConfigurationSection? Section { get; set; }

    public Guid? SubsectionId { get; set; }
    public ConfigurationSubsection? Subsection { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Optional validation rule, e.g. a regex pattern or "min:0,max:1000" convention understood by the resolver/UI.</summary>
    public string? ValidationRules { get; set; }

    public string? ExampleValue { get; set; }

    public string? HelpText { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public List<ConfigurationValue> Values { get; set; } = new();
}
