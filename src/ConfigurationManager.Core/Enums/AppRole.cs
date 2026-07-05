namespace ConfigurationManager.Core.Enums;

/// <summary>
/// Prototype role model. TODO(production): replace the API-key-to-role mapping in
/// <c>ConfigurationManager.Api/Auth</c> with Microsoft Entra ID app roles / groups.
/// </summary>
public enum AppRole
{
    /// <summary>Full read/write access to keys, values, and tenants/integrations metadata.</summary>
    Admin = 0,

    /// <summary>Can view and edit non-sensitive configuration values; cannot manage key metadata.</summary>
    ITSupport = 1,

    /// <summary>Business owner of an integration; can view and edit values scoped to their visibility group.</summary>
    Owner = 2,

    /// <summary>Runtime/service identity used by integration code to call the resolve endpoint with decrypted secrets.</summary>
    RuntimeReader = 3
}
