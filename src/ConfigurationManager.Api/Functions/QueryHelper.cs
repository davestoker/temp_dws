using System.Web;

namespace ConfigurationManager.Api.Functions;

internal static class QueryHelper
{
    public static string? Get(Uri url, string name)
    {
        var query = HttpUtility.ParseQueryString(url.Query);
        return query.Get(name);
    }

    public static Guid? GetGuid(Uri url, string name)
    {
        var value = Get(url, name);
        return Guid.TryParse(value, out var guid) ? guid : null;
    }
}
