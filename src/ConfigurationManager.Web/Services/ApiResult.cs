namespace ConfigurationManager.Web.Services;

public sealed record ApiResult<T>(bool Success, T? Data, string? Error)
{
    public static ApiResult<T> Ok(T data) => new(true, data, null);
    public static ApiResult<T> Fail(string error) => new(false, default, error);
}
