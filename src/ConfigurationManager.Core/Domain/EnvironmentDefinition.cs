namespace ConfigurationManager.Core.Domain;

/// <summary>
/// A deployment environment, e.g. Dev, UAT, Production. Named "EnvironmentDefinition"
/// to avoid colliding with System.Environment.
/// </summary>
public class EnvironmentDefinition : IAuditable
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
