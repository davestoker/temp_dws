namespace ConfigurationManager.Core.Domain;

/// <summary>Second-level grouping nested under a <see cref="ConfigurationSection"/>.</summary>
public class ConfigurationSubsection : IAuditable
{
    public Guid Id { get; set; }

    public Guid SectionId { get; set; }
    public ConfigurationSection? Section { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
