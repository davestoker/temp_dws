namespace ConfigurationManager.Api.Auth;

/// <summary>
/// One entry in the prototype's API-key-to-role map (bound from the "ApiKeys" configuration
/// section - see local.settings.json.example).
/// TODO(production): replace this entire mechanism with Microsoft Entra ID (Azure AD) bearer
/// token authentication, mapping Entra app roles / group claims to <see cref="Core.Enums.AppRole"/>,
/// and validate tokens via Microsoft.Identity.Web. Function-level identity (e.g. the runtime
/// caller) should use Managed Identity rather than a shared static key.
/// </summary>
public class ApiKeyEntry
{
    public string Key { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
