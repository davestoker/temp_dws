using System.Net;
using System.Text.Json;
using ConfigurationManager.Api.Auth;
using ConfigurationManager.Core.Dtos;
using ConfigurationManager.Core.Enums;
using ConfigurationManager.Core.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace ConfigurationManager.Api.Functions;

/// <summary>Management endpoints for configuration value overrides (the four override layers).</summary>
public class ConfigurationValueFunctions : FunctionBase
{
    private static readonly AppRole[] ReadRoles = { AppRole.Admin, AppRole.ITSupport, AppRole.Owner };
    private static readonly AppRole[] WriteRoles = { AppRole.Admin, AppRole.ITSupport, AppRole.Owner };

    private readonly ConfigurationValueService _valueService;
    private readonly ConfigurationKeyService _keyService;

    public ConfigurationValueFunctions(ConfigurationValueService valueService, ConfigurationKeyService keyService, ApiKeyAuthenticator authenticator)
        : base(authenticator)
    {
        _valueService = valueService;
        _keyService = keyService;
    }

    [Function("ListConfigurationValues")]
    public async Task<HttpResponseData> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "configuration/values")] HttpRequestData req)
    {
        var (_, error) = await AuthorizeAsync(req, ReadRoles);
        if (error is not null) return error;

        return await HandleAsync(req, async () =>
        {
            var keyId = QueryHelper.GetGuid(req.Url, "configurationKeyId");
            var tenantCode = QueryHelper.Get(req.Url, "tenantCode");
            var environmentName = QueryHelper.Get(req.Url, "environmentName");

            var values = await _valueService.ListAsync(keyId, tenantCode, environmentName, req.FunctionContext.CancellationToken);
            return await OkAsync(req, values);
        });
    }

    [Function("UpsertConfigurationValue")]
    public async Task<HttpResponseData> Upsert(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "configuration/values")] HttpRequestData req)
    {
        var (auth, error) = await AuthorizeAsync(req, WriteRoles);
        if (error is not null) return error;

        return await HandleAsync(req, async () =>
        {
            var request = await JsonSerializer.DeserializeAsync<UpsertConfigurationValueRequest>(req.Body, JsonOptions);
            if (request is null)
            {
                return await ErrorAsync(req, HttpStatusCode.BadRequest, "Request body is required.");
            }

            var ownerCheck = await CheckOwnerVisibilityAsync(req, auth!, request.ConfigurationKeyId);
            if (ownerCheck is not null)
            {
                return ownerCheck;
            }

            var result = await _valueService.UpsertAsync(request, auth!.CallerName, req.FunctionContext.CancellationToken);
            return await OkAsync(req, result);
        });
    }

    [Function("EnableConfigurationValue")]
    public async Task<HttpResponseData> Enable(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "configuration/values/{id:guid}/enable")] HttpRequestData req,
        Guid id) => await SetEnabled(req, id, true);

    [Function("DisableConfigurationValue")]
    public async Task<HttpResponseData> Disable(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "configuration/values/{id:guid}/disable")] HttpRequestData req,
        Guid id) => await SetEnabled(req, id, false);

    private async Task<HttpResponseData> SetEnabled(HttpRequestData req, Guid id, bool isEnabled)
    {
        var (auth, error) = await AuthorizeAsync(req, WriteRoles);
        if (error is not null) return error;

        return await HandleAsync(req, async () =>
            await OkAsync(req, await _valueService.SetEnabledAsync(id, isEnabled, auth!.CallerName, req.FunctionContext.CancellationToken)));
    }

    /// <summary>
    /// Owner-role callers may only write values for keys visible to the Owner group. Admin and
    /// ITSupport are unrestricted. Returns a 403 response if the check fails, otherwise null.
    /// </summary>
    private async Task<HttpResponseData?> CheckOwnerVisibilityAsync(HttpRequestData req, AuthContext auth, Guid configurationKeyId)
    {
        if (auth.Role != AppRole.Owner)
        {
            return null;
        }

        var key = await _keyService.GetAsync(configurationKeyId, req.FunctionContext.CancellationToken);
        if (key is not null && key.VisibilityGroup != VisibilityGroup.Owner)
        {
            return await ErrorAsync(req, HttpStatusCode.Forbidden, "Owner role can only edit values for keys visible to the Owner group.");
        }

        return null;
    }
}
