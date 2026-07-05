using ConfigurationManager.Core.Domain;
using ConfigurationManager.Core.Interfaces;

namespace ConfigurationManager.Core.Services;

/// <summary>
/// Writes audit log entries for configuration changes. Callers must pass already-redacted
/// old/new value strings for sensitive fields (use <see cref="Redacted"/>) - this class does
/// not know which fields are sensitive and never inspects raw secret material itself.
/// </summary>
public class AuditLogger
{
    public const string RedactedMarker = "<redacted>";

    private readonly IConfigurationAdminRepository _repository;

    public AuditLogger(IConfigurationAdminRepository repository)
    {
        _repository = repository;
    }

    public static string Redacted(bool isSensitive, string? value) => isSensitive ? RedactedMarker : value ?? string.Empty;

    public Task LogAsync(
        string entityName,
        Guid entityId,
        string action,
        string actor,
        string? oldValue = null,
        string? newValue = null,
        string? details = null,
        CancellationToken ct = default)
    {
        var entry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            OldValue = oldValue,
            NewValue = newValue,
            Details = details,
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = actor
        };

        return _repository.AddAuditLogEntryAsync(entry, ct);
    }
}
