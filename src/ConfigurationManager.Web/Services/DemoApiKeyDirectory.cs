using ConfigurationManager.Core.Enums;

namespace ConfigurationManager.Web.Services;

public class DemoApiKeyEntry
{
    public string Key { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Prototype convenience only: lets the login screen offer "sign in as Admin / IT Support /
/// Owner / Runtime Reader" without the demo user needing to know a raw API key. Bound from the
/// "ApiKeys" section of appsettings (same shape as the Api project's local.settings.json) so both
/// projects can share one set of dev keys. Not a substitute for real authentication.
/// </summary>
public class DemoApiKeyDirectory
{
    private readonly List<DemoApiKeyEntry> _entries;

    public DemoApiKeyDirectory(IConfiguration configuration)
    {
        _entries = configuration.GetSection("ApiKeys").Get<List<DemoApiKeyEntry>>() ?? new List<DemoApiKeyEntry>();
    }

    public IReadOnlyList<DemoApiKeyEntry> All => _entries;

    public DemoApiKeyEntry? FindByRole(AppRole role) =>
        _entries.FirstOrDefault(e => string.Equals(e.Role, role.ToString(), StringComparison.OrdinalIgnoreCase));
}
