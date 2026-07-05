using System.Text.Json;
using ConfigurationManager.Api.Auth;
using ConfigurationManager.Core.Dtos;
using ConfigurationManager.Core.Enums;
using ConfigurationManager.Core.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace ConfigurationManager.Api.Functions;

/// <summary>Management endpoints for configuration key metadata (not values).</summary>
public class ConfigurationKeyFunctions : FunctionBase
{
    private static readonly AppRole[] ReadRoles = { AppRole.Admin, AppRole.ITSupport, AppRole.Owner };
    private static readonly AppRole[] WriteRoles = { AppRole.Admin };

    private readonly ConfigurationKeyService _keyService;

    public ConfigurationKeyFunctions(ConfigurationKeyService keyService, ApiKeyAuthenticator authenticator)
        : base(authenticator)
    {
        _keyService = keyService;
    }

    [Function("ListConfigurationKeys")]
    public async Task<HttpResponseData> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "configuration/keys")] HttpRequestData req)
    {
        var (_, error) = await AuthorizeAsync(req, ReadRoles);
        if (error is not null) return error;

        var integrationTypeId = QueryHelper.GetGuid(req.Url, "integrationTypeId");
        var sectionId = QueryHelper.GetGuid(req.Url, "sectionId");

        return await OkAsync(req, await _keyService.ListAsync(integrationTypeId, sectionId, req.FunctionContext.CancellationToken));
    }

    [Function("GetConfigurationKey")]
    public async Task<HttpResponseData> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "configuration/keys/{id:guid}")] HttpRequestData req,
        Guid id)
    {
        var (_, error) = await AuthorizeAsync(req, ReadRoles);
        if (error is not null) return error;

        return await HandleAsync(req, async () =>
        {
            var key = await _keyService.GetAsync(id, req.FunctionContext.CancellationToken);
            return key is null
                ? await ErrorAsync(req, System.Net.HttpStatusCode.NotFound, $"Configuration key '{id}' was not found.")
                : await OkAsync(req, key);
        });
    }

    [Function("CreateConfigurationKey")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "configuration/keys")] HttpRequestData req)
    {
        var (auth, error) = await AuthorizeAsync(req, WriteRoles);
        if (error is not null) return error;

        return await HandleAsync(req, async () =>
        {
            var request = await JsonSerializer.DeserializeAsync<CreateConfigurationKeyRequest>(req.Body, JsonOptions);
            if (request is null)
            {
                return await ErrorAsync(req, System.Net.HttpStatusCode.BadRequest, "Request body is required.");
            }

            var created = await _keyService.CreateAsync(request, auth!.CallerName, req.FunctionContext.CancellationToken);
            return await CreatedAsync(req, created);
        });
    }

    [Function("UpdateConfigurationKey")]
    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "configuration/keys/{id:guid}")] HttpRequestData req,
        Guid id)
    {
        var (auth, error) = await AuthorizeAsync(req, WriteRoles);
        if (error is not null) return error;

        return await HandleAsync(req, async () =>
        {
            var request = await JsonSerializer.DeserializeAsync<UpdateConfigurationKeyRequest>(req.Body, JsonOptions);
            if (request is null)
            {
                return await ErrorAsync(req, System.Net.HttpStatusCode.BadRequest, "Request body is required.");
            }

            var updated = await _keyService.UpdateAsync(id, request, auth!.CallerName, req.FunctionContext.CancellationToken);
            return await OkAsync(req, updated);
        });
    }

    [Function("EnableConfigurationKey")]
    public async Task<HttpResponseData> Enable(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "configuration/keys/{id:guid}/enable")] HttpRequestData req,
        Guid id) => await SetEnabled(req, id, true);

    [Function("DisableConfigurationKey")]
    public async Task<HttpResponseData> Disable(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "configuration/keys/{id:guid}/disable")] HttpRequestData req,
        Guid id) => await SetEnabled(req, id, false);

    private async Task<HttpResponseData> SetEnabled(HttpRequestData req, Guid id, bool isEnabled)
    {
        var (auth, error) = await AuthorizeAsync(req, WriteRoles);
        if (error is not null) return error;

        return await HandleAsync(req, async () =>
            await OkAsync(req, await _keyService.SetEnabledAsync(id, isEnabled, auth!.CallerName, req.FunctionContext.CancellationToken)));
    }
}
