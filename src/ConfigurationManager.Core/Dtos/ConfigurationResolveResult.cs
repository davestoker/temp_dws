namespace ConfigurationManager.Core.Dtos;

public enum ConfigurationResolveStatus
{
    Ok,
    TenantNotFound,
    EnvironmentNotFound,
    IntegrationNotFound
}

/// <summary>Result of resolving configuration for a tenant/integration/environment triple.</summary>
public sealed class ConfigurationResolveResult
{
    public required ConfigurationResolveStatus Status { get; init; }

    public IReadOnlyList<ResolvedConfigurationItem> Items { get; init; } = Array.Empty<ResolvedConfigurationItem>();

    /// <summary>True if any required key resolved as missing/disabled.</summary>
    public bool HasMissingRequiredValues => Items.Any(i => i.IsMissing);

    public static ConfigurationResolveResult NotFound(ConfigurationResolveStatus status) => new()
    {
        Status = status,
        Items = Array.Empty<ResolvedConfigurationItem>()
    };
}
