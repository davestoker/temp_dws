using System.Net;
using ConfigurationManager.Api.Auth;
using ConfigurationManager.Core.Dtos;
using ConfigurationManager.Core.Enums;
using ConfigurationManager.Core.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ConfigurationManager.Api.Functions;

/// <summary>
/// Runtime + safe resolve endpoints. Two distinct routes rather than a query-string "reveal"
/// flag, so an unauthorised caller can never flip a switch to obtain decrypted secrets -
/// the decrypted path requires the RuntimeReader (or Admin) role by construction.
/// </summary>
public class ResolveConfigurationFunctions : FunctionBase
{
    private readonly IConfigurationResolver _resolver;
    private readonly ILogger<ResolveConfigurationFunctions> _logger;

    public ResolveConfigurationFunctions(IConfigurationResolver resolver, ApiKeyAuthenticator authenticator, ILogger<ResolveConfigurationFunctions> logger)
        : base(authenticator)
    {
        _resolver = resolver;
        _logger = logger;
    }

    /// <summary>Safe/UI mode: sensitive values are always masked. Any authenticated role may call this.</summary>
    [Function("ResolveConfiguration")]
    public async Task<HttpResponseData> ResolveSafe(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "configuration/resolve")] HttpRequestData req)
    {
        var (auth, error) = await AuthorizeAsync(req, AppRole.Admin, AppRole.ITSupport, AppRole.Owner, AppRole.RuntimeReader);
        if (error is not null)
        {
            return error;
        }

        return await ResolveAsync(req, revealSecrets: false);
    }

    /// <summary>Authorised runtime mode: sensitive values are decrypted. Restricted to RuntimeReader and Admin.</summary>
    [Function("ResolveConfigurationRuntime")]
    public async Task<HttpResponseData> ResolveRuntime(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "runtime/configuration/resolve")] HttpRequestData req)
    {
        var (auth, error) = await AuthorizeAsync(req, AppRole.RuntimeReader, AppRole.Admin);
        if (error is not null)
        {
            return error;
        }

        _logger.LogInformation("Runtime configuration resolve requested by {Caller}", auth!.CallerName);
        return await ResolveAsync(req, revealSecrets: true);
    }

    private async Task<HttpResponseData> ResolveAsync(HttpRequestData req, bool revealSecrets)
    {
        return await HandleAsync(req, async () =>
        {
            var tenantCode = QueryHelper.Get(req.Url, "tenantCode");
            var integrationName = QueryHelper.Get(req.Url, "integrationName");
            var environmentName = QueryHelper.Get(req.Url, "environmentName");

            if (string.IsNullOrWhiteSpace(tenantCode) || string.IsNullOrWhiteSpace(integrationName) || string.IsNullOrWhiteSpace(environmentName))
            {
                return await ErrorAsync(req, HttpStatusCode.BadRequest,
                    "tenantCode, integrationName, and environmentName query parameters are all required.");
            }

            var result = await _resolver.ResolveAsync(tenantCode, integrationName, environmentName, revealSecrets, req.FunctionContext.CancellationToken);

            if (result.Status != ConfigurationResolveStatus.Ok)
            {
                return await ErrorAsync(req, HttpStatusCode.NotFound, result.Status switch
                {
                    ConfigurationResolveStatus.TenantNotFound => $"Tenant '{tenantCode}' was not found or is disabled.",
                    ConfigurationResolveStatus.EnvironmentNotFound => $"Environment '{environmentName}' was not found or is disabled.",
                    ConfigurationResolveStatus.IntegrationNotFound => $"Integration '{integrationName}' was not found or is disabled.",
                    _ => "Unable to resolve configuration."
                });
            }

            return await OkAsync(req, result);
        });
    }
}
