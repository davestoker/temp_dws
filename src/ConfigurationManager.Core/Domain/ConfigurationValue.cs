namespace ConfigurationManager.Core.Domain;

/// <summary>
/// A single override row for a configuration key. The combination of (TenantId, EnvironmentId)
/// being null/non-null identifies which layer the row belongs to:
///   TenantId = null, EnvironmentId = null            -> Base
///   TenantId = X,    EnvironmentId = null            -> Tenant
///   TenantId = null, EnvironmentId = Y                -> Environment
///   TenantId = X,    EnvironmentId = Y                -> TenantEnvironment
/// Exactly one row may exist per (ConfigurationKeyId, TenantId, EnvironmentId) combination.
/// </summary>
public class ConfigurationValue : IAuditable
{
    public Guid Id { get; set; }

    public Guid ConfigurationKeyId { get; set; }
    public ConfigurationKey? ConfigurationKey { get; set; }

    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid? EnvironmentId { get; set; }
    public EnvironmentDefinition? Environment { get; set; }

    /// <summary>Plain-text value. Null when the key is sensitive; use <see cref="EncryptedValue"/> instead.</summary>
    public string? Value { get; set; }

    /// <summary>Ciphertext (base64) produced by <see cref="Interfaces.ISecretProtector"/>. Set only for sensitive keys.</summary>
    public string? EncryptedValue { get; set; }

    /// <summary>Whether this specific override is active. Disabled overrides are skipped by the resolver, which falls back to the next less-specific enabled layer.</summary>
    public bool IsEnabled { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
