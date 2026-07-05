using ConfigurationManager.Core.Enums;

namespace ConfigurationManager.Web.Services;

/// <summary>
/// Prototype "sign-in" state for the current Blazor Server circuit (scoped per user connection).
/// A real implementation would come from Microsoft Entra ID authentication; this simply lets the
/// demo user pick which role they want to browse as, and holds the matching API key so the
/// typed HTTP client can call the Functions API as that role.
/// TODO(production): replace with Entra ID sign-in + claims-based role mapping.
/// </summary>
public class CurrentSession
{
    public bool IsSignedIn { get; private set; }
    public AppRole Role { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string ApiKey { get; private set; } = string.Empty;

    public event Action? Changed;

    public void SignIn(AppRole role, string displayName, string apiKey)
    {
        Role = role;
        DisplayName = displayName;
        ApiKey = apiKey;
        IsSignedIn = true;
        Changed?.Invoke();
    }

    public void SignOut()
    {
        IsSignedIn = false;
        DisplayName = string.Empty;
        ApiKey = string.Empty;
        Changed?.Invoke();
    }
}
