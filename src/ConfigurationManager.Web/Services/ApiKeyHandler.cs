namespace ConfigurationManager.Web.Services;

/// <summary>Attaches the current session's API key to every outgoing request to the Functions API.</summary>
public class ApiKeyHandler : DelegatingHandler
{
    private readonly CurrentSession _session;

    public ApiKeyHandler(CurrentSession session)
    {
        _session = session;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_session.ApiKey))
        {
            request.Headers.Remove("X-Api-Key");
            request.Headers.Add("X-Api-Key", _session.ApiKey);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
