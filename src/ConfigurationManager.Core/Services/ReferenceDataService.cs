using ConfigurationManager.Core.Dtos;
using ConfigurationManager.Core.Interfaces;

namespace ConfigurationManager.Core.Services;

/// <summary>Read-only lookups for tenants, environments, integration types, integrations, and sections.</summary>
public class ReferenceDataService
{
    private readonly IConfigurationAdminRepository _repository;

    public ReferenceDataService(IConfigurationAdminRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<TenantDto>> ListTenantsAsync(CancellationToken ct = default) =>
        (await _repository.ListTenantsAsync(ct)).Select(t => new TenantDto(t.Id, t.TenantCode, t.Name, t.IsEnabled)).ToList();

    public async Task<IReadOnlyList<EnvironmentDto>> ListEnvironmentsAsync(CancellationToken ct = default) =>
        (await _repository.ListEnvironmentsAsync(ct)).Select(e => new EnvironmentDto(e.Id, e.Name, e.SortOrder, e.IsEnabled)).ToList();

    public async Task<IReadOnlyList<IntegrationTypeDto>> ListIntegrationTypesAsync(CancellationToken ct = default) =>
        (await _repository.ListIntegrationTypesAsync(ct)).Select(i => new IntegrationTypeDto(i.Id, i.Name, i.Description, i.IsEnabled)).ToList();

    public async Task<IReadOnlyList<IntegrationDto>> ListIntegrationsAsync(CancellationToken ct = default) =>
        (await _repository.ListIntegrationsAsync(ct))
            .Select(i => new IntegrationDto(i.Id, i.IntegrationName, i.IntegrationTypeId, i.IntegrationType?.Name ?? string.Empty, i.Description, i.IsEnabled))
            .ToList();

    public async Task<IReadOnlyList<ConfigurationSectionDto>> ListSectionsAsync(CancellationToken ct = default) =>
        (await _repository.ListSectionsWithSubsectionsAsync(ct))
            .Select(s => new ConfigurationSectionDto(
                s.Id,
                s.Name,
                s.SortOrder,
                s.Subsections.OrderBy(sub => sub.SortOrder).Select(sub => new ConfigurationSubsectionDto(sub.Id, sub.Name, sub.SortOrder)).ToList()))
            .ToList();
}
