using ConfigurationManager.Core.Enums;

namespace ConfigurationManager.Api.Auth;

/// <summary>The authenticated caller for the current request, or an explanation of why authentication failed.</summary>
public sealed class AuthContext
{
    public bool IsAuthenticated { get; init; }
    public AppRole Role { get; init; }
    public string CallerName { get; init; } = string.Empty;
    public string? FailureReason { get; init; }

    public static AuthContext Failed(string reason) => new() { IsAuthenticated = false, FailureReason = reason };

    public static AuthContext Success(AppRole role, string callerName) => new()
    {
        IsAuthenticated = true,
        Role = role,
        CallerName = callerName
    };
}
