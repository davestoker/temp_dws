namespace ConfigurationManager.Core.Domain;

/// <summary>
/// Immutable record of a change made through the management API. Sensitive values are
/// never written here in plain text - see <see cref="AuditLogger"/> in Core.Services.
/// </summary>
public class AuditLogEntry
{
    public Guid Id { get; set; }

    /// <summary>e.g. "ConfigurationKey", "ConfigurationValue", "Tenant".</summary>
    public string EntityName { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    /// <summary>e.g. "Created", "Updated", "Enabled", "Disabled", "SecretReplaced".</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Redacted (masked) representation of the previous state, safe to display.</summary>
    public string? OldValue { get; set; }

    /// <summary>Redacted (masked) representation of the new state, safe to display.</summary>
    public string? NewValue { get; set; }

    public string? Details { get; set; }

    public DateTimeOffset ChangedAt { get; set; }

    public string ChangedBy { get; set; } = string.Empty;
}
