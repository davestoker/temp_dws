namespace ConfigurationManager.Core.Domain;

/// <summary>Top-level grouping used to organise configuration keys in the management UI.</summary>
public class ConfigurationSection : IAuditable
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public List<ConfigurationSubsection> Subsections { get; set; } = new();
}
