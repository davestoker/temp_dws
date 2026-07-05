using ConfigurationManager.Core.Enums;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;

namespace ConfigurationManager.Api.Auth;

/// <summary>
/// Prototype authentication: looks up a static API key from the "X-Api-Key" header against a
/// configured key-to-role map. This is intentionally simple so it can be swapped later for
/// Microsoft Entra ID without changing any function signatures - callers only interact with
/// <see cref="AuthContext"/>. Never logs the presented key.
/// </summary>
public class ApiKeyAuthenticator
{
    private const string ApiKeyHeader = "X-Api-Key";

    private readonly Dictionary<string, ApiKeyEntry> _keysByValue;

    public ApiKeyAuthenticator(IOptions<List<ApiKeyEntry>> options)
    {
        _keysByValue = (options.Value ?? new List<ApiKeyEntry>())
            .Where(e => !string.IsNullOrWhiteSpace(e.Key))
            .ToDictionary(e => e.Key, StringComparer.Ordinal);
    }

    public AuthContext Authenticate(HttpRequestData request)
    {
        if (!request.Headers.TryGetValues(ApiKeyHeader, out var values))
        {
            return AuthContext.Failed($"Missing '{ApiKeyHeader}' header.");
        }

        var presentedKey = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(presentedKey) || !_keysByValue.TryGetValue(presentedKey, out var entry))
        {
            return AuthContext.Failed("Invalid API key.");
        }

        if (!Enum.TryParse<AppRole>(entry.Role, ignoreCase: true, out var role))
        {
            return AuthContext.Failed($"API key is configured with an unrecognised role '{entry.Role}'.");
        }

        return AuthContext.Success(role, string.IsNullOrWhiteSpace(entry.Name) ? "unknown-caller" : entry.Name);
    }
}
