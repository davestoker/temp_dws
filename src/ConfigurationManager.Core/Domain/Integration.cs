namespace ConfigurationManager.Core.Domain;

/// <summary>
/// A named integration instance that runtime callers identify themselves with
/// (the "IntegrationName" filter dimension), e.g. "EngagingNetworksSync".
/// Belongs to an <see cref="Domain.IntegrationType"/> which determines which
/// integration-type-scoped configuration keys apply to it.
/// </summary>
public class Integration : IAuditable
{
    public Guid Id { get; set; }

    /// <summary>Unique name used by integration code when calling the resolve API.</summary>
    public string IntegrationName { get; set; } = string.Empty;

    public Guid IntegrationTypeId { get; set; }
    public IntegrationType? IntegrationType { get; set; }

    public string? Description { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
