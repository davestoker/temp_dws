namespace ConfigurationManager.Core.Dtos;

public sealed record TenantDto(Guid Id, string TenantCode, string Name, bool IsEnabled);

public sealed record EnvironmentDto(Guid Id, string Name, int SortOrder, bool IsEnabled);

public sealed record IntegrationTypeDto(Guid Id, string Name, string? Description, bool IsEnabled);

public sealed record IntegrationDto(Guid Id, string IntegrationName, Guid IntegrationTypeId, string IntegrationTypeName, string? Description, bool IsEnabled);

public sealed record ConfigurationSubsectionDto(Guid Id, string Name, int SortOrder);

public sealed record ConfigurationSectionDto(Guid Id, string Name, int SortOrder, IReadOnlyList<ConfigurationSubsectionDto> Subsections);
