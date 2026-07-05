namespace ConfigurationManager.Core.Domain;

/// <summary>A customer / organisation that runs one or more integrations.</summary>
public class Tenant : IAuditable
{
    public Guid Id { get; set; }

    /// <summary>Short, stable, unique identifier used in APIs and URLs, e.g. "acme".</summary>
    public string TenantCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
