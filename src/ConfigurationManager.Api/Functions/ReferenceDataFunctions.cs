using ConfigurationManager.Api.Auth;
using ConfigurationManager.Core.Enums;
using ConfigurationManager.Core.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace ConfigurationManager.Api.Functions;

/// <summary>Read-only lookups used to populate the management UI's pickers/filters.</summary>
public class ReferenceDataFunctions : FunctionBase
{
    private static readonly AppRole[] AnyAuthenticatedRole =
        { AppRole.Admin, AppRole.ITSupport, AppRole.Owner, AppRole.RuntimeReader };

    private readonly ReferenceDataService _referenceDataService;

    public ReferenceDataFunctions(ReferenceDataService referenceDataService, ApiKeyAuthenticator authenticator)
        : base(authenticator)
    {
        _referenceDataService = referenceDataService;
    }

    [Function("ListTenants")]
    public async Task<HttpResponseData> ListTenants(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tenants")] HttpRequestData req)
    {
        var (_, error) = await AuthorizeAsync(req, AnyAuthenticatedRole);
        if (error is not null) return error;

        return await OkAsync(req, await _referenceDataService.ListTenantsAsync(req.FunctionContext.CancellationToken));
    }

    [Function("ListEnvironments")]
    public async Task<HttpResponseData> ListEnvironments(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "environments")] HttpRequestData req)
    {
        var (_, error) = await AuthorizeAsync(req, AnyAuthenticatedRole);
        if (error is not null) return error;

        return await OkAsync(req, await _referenceDataService.ListEnvironmentsAsync(req.FunctionContext.CancellationToken));
    }

    [Function("ListIntegrationTypes")]
    public async Task<HttpResponseData> ListIntegrationTypes(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "integrationtypes")] HttpRequestData req)
    {
        var (_, error) = await AuthorizeAsync(req, AnyAuthenticatedRole);
        if (error is not null) return error;

        return await OkAsync(req, await _referenceDataService.ListIntegrationTypesAsync(req.FunctionContext.CancellationToken));
    }

    [Function("ListIntegrations")]
    public async Task<HttpResponseData> ListIntegrations(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "integrations")] HttpRequestData req)
    {
        var (_, error) = await AuthorizeAsync(req, AnyAuthenticatedRole);
        if (error is not null) return error;

        return await OkAsync(req, await _referenceDataService.ListIntegrationsAsync(req.FunctionContext.CancellationToken));
    }

    [Function("ListSections")]
    public async Task<HttpResponseData> ListSections(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sections")] HttpRequestData req)
    {
        var (_, error) = await AuthorizeAsync(req, AnyAuthenticatedRole);
        if (error is not null) return error;

        return await OkAsync(req, await _referenceDataService.ListSectionsAsync(req.FunctionContext.CancellationToken));
    }
}
