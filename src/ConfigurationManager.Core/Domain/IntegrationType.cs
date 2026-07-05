namespace ConfigurationManager.Core.Domain;

/// <summary>
/// The kind of external system an integration talks to, e.g. EngagingNetworks, Cybertill, Shopify.
/// Configuration keys can be scoped to an integration type to define the "Base" layer of the
/// override model; keys with a null IntegrationTypeId are considered global (apply to any integration).
/// </summary>
public class IntegrationType : IAuditable
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
