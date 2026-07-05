namespace ConfigurationManager.Core.Services;

/// <summary>Thrown for input validation failures in the management services; API layer maps this to HTTP 400.</summary>
public class ConfigurationValidationException : Exception
{
    public ConfigurationValidationException(string message) : base(message)
    {
    }
}

/// <summary>Thrown when a referenced entity (key, tenant, environment, etc.) does not exist; API layer maps this to HTTP 404.</summary>
public class ConfigurationNotFoundException : Exception
{
    public ConfigurationNotFoundException(string message) : base(message)
    {
    }
}
