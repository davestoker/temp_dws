namespace ConfigurationManager.Core.Enums;

/// <summary>
/// Identifies which override layer supplied the resolved value for a configuration key.
/// Ordered from least to most specific; the resolver picks the highest-ranking layer
/// that has an enabled value. See docs/database-design.md for the full precedence rules.
/// </summary>
public enum ConfigurationSourceLayer
{
    /// <summary>No value was found at any layer, and no key-level default applies.</summary>
    None = 0,

    /// <summary>Key-level metadata default (ConfigurationKey.DefaultValue), used when no override row exists at all.</summary>
    KeyDefault = 1,

    /// <summary>Base value for the integration type (Tenant = null, Environment = null).</summary>
    Base = 2,

    /// <summary>Tenant-specific override, applies across all environments (Tenant = X, Environment = null).</summary>
    Tenant = 3,

    /// <summary>Environment-specific override, applies across all tenants (Tenant = null, Environment = Y).</summary>
    Environment = 4,

    /// <summary>Most specific: tenant + environment override (Tenant = X, Environment = Y).</summary>
    TenantEnvironment = 5
}
