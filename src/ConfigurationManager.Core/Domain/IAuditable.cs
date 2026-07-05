namespace ConfigurationManager.Core.Domain;

/// <summary>Common audit fields applied to every editable entity.</summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    string CreatedBy { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}
