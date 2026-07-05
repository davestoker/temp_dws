using ConfigurationManager.Core.Domain;
using ConfigurationManager.Core.Enums;
using ConfigurationManager.Core.Interfaces;
using ConfigurationManager.Core.Services;
using ConfigurationManager.Data;
using ConfigurationManager.Data.Repositories;

namespace ConfigurationManager.Tests;

/// <summary>
/// Builds a minimal, hand-populated set of reference data (one tenant, one environment, one
/// integration type/integration) plus a single configuration key, so each test can add exactly
/// the ConfigurationValue rows it needs to exercise one resolver scenario.
/// </summary>
internal sealed class ResolverTestFixture
{
    public ConfigurationDbContext Db { get; }
    public ISecretProtector SecretProtector { get; }
    public IConfigurationResolver Resolver { get; }

    public Tenant Tenant { get; }
    public Tenant OtherTenant { get; }
    public EnvironmentDefinition Environment { get; }
    public EnvironmentDefinition OtherEnvironment { get; }
    public IntegrationType IntegrationType { get; }
    public Integration Integration { get; }

    private const string Actor = "test";

    public ResolverTestFixture()
    {
        Db = TestDbContextFactory.Create();
        SecretProtector = TestSecretProtectorFactory.Create();

        var repository = new EfConfigurationRepository(Db);
        Resolver = new ConfigurationResolver(repository, SecretProtector);

        var now = DateTimeOffset.UtcNow;

        Tenant = new Tenant { Id = Guid.NewGuid(), TenantCode = "acme", Name = "Acme", IsEnabled = true, CreatedAt = now, CreatedBy = Actor };
        OtherTenant = new Tenant { Id = Guid.NewGuid(), TenantCode = "other", Name = "Other", IsEnabled = true, CreatedAt = now, CreatedBy = Actor };
        Environment = new EnvironmentDefinition { Id = Guid.NewGuid(), Name = "Dev", SortOrder = 1, IsEnabled = true, CreatedAt = now, CreatedBy = Actor };
        OtherEnvironment = new EnvironmentDefinition { Id = Guid.NewGuid(), Name = "Production", SortOrder = 2, IsEnabled = true, CreatedAt = now, CreatedBy = Actor };
        IntegrationType = new IntegrationType { Id = Guid.NewGuid(), Name = "EngagingNetworks", IsEnabled = true, CreatedAt = now, CreatedBy = Actor };
        Integration = new Integration
        {
            Id = Guid.NewGuid(), IntegrationName = "EngagingNetworksSync", IntegrationTypeId = IntegrationType.Id,
            IsEnabled = true, CreatedAt = now, CreatedBy = Actor
        };

        Db.Tenants.AddRange(Tenant, OtherTenant);
        Db.Environments.AddRange(Environment, OtherEnvironment);
        Db.IntegrationTypes.Add(IntegrationType);
        Db.Integrations.Add(Integration);
        Db.SaveChanges();
    }

    public ConfigurationKey AddKey(string name, bool isRequired = false, bool isSensitive = false, string? defaultValue = null)
    {
        var key = new ConfigurationKey
        {
            Id = Guid.NewGuid(),
            KeyName = name,
            DisplayName = name,
            DataType = isSensitive ? ConfigurationDataType.Secret : ConfigurationDataType.String,
            IsRequired = isRequired,
            IsSensitive = isSensitive,
            IsEnabled = true,
            DefaultValue = defaultValue,
            IntegrationTypeId = IntegrationType.Id,
            SortOrder = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Actor
        };
        Db.ConfigurationKeys.Add(key);
        Db.SaveChanges();
        return key;
    }

    public ConfigurationValue AddValue(ConfigurationKey key, Guid? tenantId, Guid? environmentId, string? plainValue, bool isEnabled = true)
    {
        var value = new ConfigurationValue
        {
            Id = Guid.NewGuid(),
            ConfigurationKeyId = key.Id,
            TenantId = tenantId,
            EnvironmentId = environmentId,
            IsEnabled = isEnabled,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Actor
        };

        if (key.IsSensitive)
        {
            value.EncryptedValue = plainValue is null ? null : SecretProtector.Protect(plainValue);
        }
        else
        {
            value.Value = plainValue;
        }

        Db.ConfigurationValues.Add(value);
        Db.SaveChanges();
        return value;
    }
}
