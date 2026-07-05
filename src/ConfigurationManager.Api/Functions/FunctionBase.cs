using System.Net;
using System.Text.Json;
using ConfigurationManager.Api.Auth;
using ConfigurationManager.Core.Enums;
using ConfigurationManager.Core.Services;
using Microsoft.Azure.Functions.Worker.Http;

namespace ConfigurationManager.Api.Functions;

/// <summary>Shared helpers for HTTP-triggered functions: auth checks and JSON responses. Thin by design - business logic lives in Core.Services.</summary>
public abstract class FunctionBase
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApiKeyAuthenticator _authenticator;

    protected FunctionBase(ApiKeyAuthenticator authenticator)
    {
        _authenticator = authenticator;
    }

    /// <summary>
    /// Authenticates the request and checks the caller's role is one of <paramref name="allowedRoles"/>.
    /// Returns null and an authorized AuthContext on success; returns a 401/403 response otherwise.
    /// </summary>
    protected async Task<(AuthContext? Auth, HttpResponseData? ErrorResponse)> AuthorizeAsync(
        HttpRequestData request, params AppRole[] allowedRoles)
    {
        var auth = _authenticator.Authenticate(request);
        if (!auth.IsAuthenticated)
        {
            return (null, await ErrorAsync(request, HttpStatusCode.Unauthorized, auth.FailureReason ?? "Not authenticated."));
        }

        if (allowedRoles.Length > 0 && !allowedRoles.Contains(auth.Role))
        {
            return (null, await ErrorAsync(request, HttpStatusCode.Forbidden,
                $"Role '{auth.Role}' is not permitted to perform this action."));
        }

        return (auth, null);
    }

    protected Task<HttpResponseData> OkAsync<T>(HttpRequestData request, T body) =>
        WriteJsonAsync(request, HttpStatusCode.OK, body);

    protected Task<HttpResponseData> CreatedAsync<T>(HttpRequestData request, T body) =>
        WriteJsonAsync(request, HttpStatusCode.Created, body);

    // Deliberately generic: never include exception details, stack traces, or config values in error responses.
    protected static Task<HttpResponseData> ErrorAsync(HttpRequestData request, HttpStatusCode statusCode, string message) =>
        WriteJsonAsync(request, statusCode, new { error = message });

    private static async Task<HttpResponseData> WriteJsonAsync<T>(HttpRequestData request, HttpStatusCode statusCode, T body)
    {
        var response = request.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await JsonSerializer.SerializeAsync(response.Body, body, JsonOptions);
        return response;
    }

    protected async Task<HttpResponseData> HandleAsync(HttpRequestData request, Func<Task<HttpResponseData>> action)
    {
        try
        {
            return await action();
        }
        catch (ConfigurationValidationException ex)
        {
            return await ErrorAsync(request, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (ConfigurationNotFoundException ex)
        {
            return await ErrorAsync(request, HttpStatusCode.NotFound, ex.Message);
        }
    }
}
