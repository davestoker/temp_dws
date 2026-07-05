using System.Net.Http.Json;
using System.Text.Json;
using ConfigurationManager.Core.Dtos;

namespace ConfigurationManager.Web.Services;

/// <summary>
/// Thin typed client for the ConfigurationManager.Api Functions app. All business logic lives
/// server-side; this class only shapes HTTP calls. The current user's API key is attached per
/// request from <see cref="CurrentSession"/> - injected directly here (typed clients are
/// resolved from the Blazor circuit's DI scope) rather than via a DelegatingHandler, because
/// IHttpClientFactory builds handler pipelines in its own DI scope and would receive a
/// different, empty CurrentSession instance.
/// </summary>
public class ConfigurationApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly CurrentSession _session;

    public ConfigurationApiClient(HttpClient http, CurrentSession session)
    {
        _http = http;
        _session = session;
    }

    public Task<ApiResult<IReadOnlyList<TenantDto>>> GetTenantsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<TenantDto>>("tenants", ct);

    public Task<ApiResult<IReadOnlyList<EnvironmentDto>>> GetEnvironmentsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<EnvironmentDto>>("environments", ct);

    public Task<ApiResult<IReadOnlyList<IntegrationTypeDto>>> GetIntegrationTypesAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<IntegrationTypeDto>>("integrationtypes", ct);

    public Task<ApiResult<IReadOnlyList<IntegrationDto>>> GetIntegrationsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<IntegrationDto>>("integrations", ct);

    public Task<ApiResult<IReadOnlyList<ConfigurationSectionDto>>> GetSectionsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<ConfigurationSectionDto>>("sections", ct);

    public Task<ApiResult<IReadOnlyList<ConfigurationKeyDto>>> GetKeysAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<ConfigurationKeyDto>>("configuration/keys", ct);

    public Task<ApiResult<ConfigurationResolveResult>> ResolveAsync(string tenantCode, string integrationName, string environmentName, CancellationToken ct = default) =>
        GetAsync<ConfigurationResolveResult>(
            $"configuration/resolve?tenantCode={Uri.EscapeDataString(tenantCode)}&integrationName={Uri.EscapeDataString(integrationName)}&environmentName={Uri.EscapeDataString(environmentName)}",
            ct);

    public Task<ApiResult<IReadOnlyList<ConfigurationValueDto>>> GetValuesAsync(Guid configurationKeyId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<ConfigurationValueDto>>($"configuration/values?configurationKeyId={configurationKeyId}", ct);

    public Task<ApiResult<ConfigurationValueDto>> UpsertValueAsync(UpsertConfigurationValueRequest request, CancellationToken ct = default) =>
        PostAsync<ConfigurationValueDto>("configuration/values", request, ct);

    public Task<ApiResult<ConfigurationValueDto>> SetValueEnabledAsync(Guid id, bool isEnabled, CancellationToken ct = default) =>
        PostAsync<ConfigurationValueDto>($"configuration/values/{id}/{(isEnabled ? "enable" : "disable")}", null, ct);

    public Task<ApiResult<ConfigurationKeyDto>> CreateKeyAsync(CreateConfigurationKeyRequest request, CancellationToken ct = default) =>
        PostAsync<ConfigurationKeyDto>("configuration/keys", request, ct);

    public Task<ApiResult<ConfigurationKeyDto>> SetKeyEnabledAsync(Guid id, bool isEnabled, CancellationToken ct = default) =>
        PostAsync<ConfigurationKeyDto>($"configuration/keys/{id}/{(isEnabled ? "enable" : "disable")}", null, ct);

    private async Task<ApiResult<T>> GetAsync<T>(string url, CancellationToken ct)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Get, url);
            using var response = await _http.SendAsync(request, ct);
            return await ReadResultAsync<T>(response, ct);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<T>.Fail($"Could not reach the configuration API: {ex.Message}");
        }
    }

    private async Task<ApiResult<T>> PostAsync<T>(string url, object? body, CancellationToken ct)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Post, url);
            request.Content = JsonContent.Create(body, options: JsonOptions);
            using var response = await _http.SendAsync(request, ct);
            return await ReadResultAsync<T>(response, ct);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<T>.Fail($"Could not reach the configuration API: {ex.Message}");
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrEmpty(_session.ApiKey))
        {
            request.Headers.Add("X-Api-Key", _session.ApiKey);
        }

        return request;
    }

    private static async Task<ApiResult<T>> ReadResultAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            string message = $"Request failed with status {(int)response.StatusCode}.";
            try
            {
                var problem = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions, ct);
                if (!string.IsNullOrWhiteSpace(problem?.Error))
                {
                    message = problem!.Error;
                }
            }
            catch (JsonException)
            {
                // Ignore - fall back to the generic status-code message.
            }

            return ApiResult<T>.Fail(message);
        }

        var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
        return data is null ? ApiResult<T>.Fail("Empty response from server.") : ApiResult<T>.Ok(data);
    }

    private sealed class ErrorPayload
    {
        public string? Error { get; set; }
    }
}
