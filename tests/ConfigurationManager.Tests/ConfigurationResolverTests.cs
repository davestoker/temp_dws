using ConfigurationManager.Core.Enums;
using ConfigurationManager.Core.Services;
using Xunit;

namespace ConfigurationManager.Tests;

public class ConfigurationResolverTests
{
    [Fact]
    public async Task BaseValueOnly_IsReturnedWhenNoOverridesExist()
    {
        var fx = new ResolverTestFixture();
        var key = fx.AddKey("Some.Key");
        fx.AddValue(key, tenantId: null, environmentId: null, "base-value");

        var result = await fx.Resolver.ResolveAsync(fx.Tenant.TenantCode, fx.Integration.IntegrationName, fx.Environment.Name);

        var item = Assert.Single(result.Items);
        Assert.Equal("base-value", item.Value);
        Assert.Equal(ConfigurationSourceLayer.Base, item.SourceLayer);
    }

    [Fact]
    public async Task TenantOverride_WinsOverBaseValue()
    {
        var fx = new ResolverTestFixture();
        var key = fx.AddKey("Some.Key");
        fx.AddValue(key, tenantId: null, environmentId: null, "base-value");
        fx.AddValue(key, tenantId: fx.Tenant.Id, environmentId: null, "tenant-value");

        var result = await fx.Resolver.ResolveAsync(fx.Tenant.TenantCode, fx.Integration.IntegrationName, fx.Environment.Name);

        var item = Assert.Single(result.Items);
        Assert.Equal("tenant-value", item.Value);
        Assert.Equal(ConfigurationSourceLayer.Tenant, item.SourceLayer);
    }

    [Fact]
    public async Task EnvironmentOverride_WinsOverTenantOverride()
    {
        var fx = new ResolverTestFixture();
        var key = fx.AddKey("Some.Key");
        fx.AddValue(key, tenantId: null, environmentId: null, "base-value");
        fx.AddValue(key, tenantId: fx.Tenant.Id, environmentId: null, "tenant-value");
        fx.AddValue(key, tenantId: null, environmentId: fx.Environment.Id, "environment-value");

        var result = await fx.Resolver.ResolveAsync(fx.Tenant.TenantCode, fx.Integration.IntegrationName, fx.Environment.Name);

        var item = Assert.Single(result.Items);
        Assert.Equal("environment-value", item.Value);
        Assert.Equal(ConfigurationSourceLayer.Environment, item.SourceLayer);
    }

    [Fact]
    public async Task TenantEnvironmentOverride_IsMostSpecificAndWins()
    {
        var fx = new ResolverTestFixture();
        var key = fx.AddKey("Some.Key");
        fx.AddValue(key, tenantId: null, environmentId: null, "base-value");
        fx.AddValue(key, tenantId: fx.Tenant.Id, environmentId: null, "tenant-value");
        fx.AddValue(key, tenantId: null, environmentId: fx.Environment.Id, "environment-value");
        fx.AddValue(key, tenantId: fx.Tenant.Id, environmentId: fx.Environment.Id, "tenant-environment-value");

        var result = await fx.Resolver.ResolveAsync(fx.Tenant.TenantCode, fx.Integration.IntegrationName, fx.Environment.Name);

        var item = Assert.Single(result.Items);
        Assert.Equal("tenant-environment-value", item.Value);
        Assert.Equal(ConfigurationSourceLayer.TenantEnvironment, item.SourceLayer);
    }

    [Fact]
    public async Task DisabledOverride_FallsBackToNextLessSpecificEnabledLayer()
    {
        var fx = new ResolverTestFixture();
        var key = fx.AddKey("Some.Key");
        fx.AddValue(key, tenantId: null, environmentId: null, "base-value");
        fx.AddValue(key, tenantId: fx.Tenant.Id, environmentId: null, "tenant-value");
        // The most specific override exists but is disabled - should fall back to Tenant layer, not Environment/Base.
        fx.AddValue(key, tenantId: fx.Tenant.Id, environmentId: fx.Environment.Id, "disabled-value", isEnabled: false);

        var result = await fx.Resolver.ResolveAsync(fx.Tenant.TenantCode, fx.Integration.IntegrationName, fx.Environment.Name);

        var item = Assert.Single(result.Items);
        Assert.Equal("tenant-value", item.Value);
        Assert.Equal(ConfigurationSourceLayer.Tenant, item.SourceLayer);
        Assert.True(item.IsEnabled);
    }

    [Fact]
    public async Task MissingRequiredValue_IsReportedAsMissing()
    {
        var fx = new ResolverTestFixture();
        var key = fx.AddKey("Some.Required.Key", isRequired: true);
        // No ConfigurationValue rows at all, and no DefaultValue.

        var result = await fx.Resolver.ResolveAsync(fx.Tenant.TenantCode, fx.Integration.IntegrationName, fx.Environment.Name);

        var item = Assert.Single(result.Items);
        Assert.Null(item.Value);
        Assert.True(item.IsMissing);
        Assert.False(item.IsEnabled);
        Assert.Equal(ConfigurationSourceLayer.None, item.SourceLayer);
        Assert.True(result.HasMissingRequiredValues);
    }

    [Fact]
    public async Task SensitiveValue_IsMaskedInSafeUiMode()
    {
        var fx = new ResolverTestFixture();
        var key = fx.AddKey("Some.Secret", isSensitive: true);
        fx.AddValue(key, tenantId: null, environmentId: null, "super-secret-value");

        var result = await fx.Resolver.ResolveAsync(fx.Tenant.TenantCode, fx.Integration.IntegrationName, fx.Environment.Name, revealSecrets: false);

        var item = Assert.Single(result.Items);
        Assert.Equal(ConfigurationResolver.MaskedValue, item.Value);
        Assert.DoesNotContain("super-secret-value", item.Value);
        Assert.True(item.IsSensitive);
    }

    [Fact]
    public async Task SensitiveValue_IsDecryptedOnlyInAuthorisedRuntimeMode()
    {
        var fx = new ResolverTestFixture();
        var key = fx.AddKey("Some.Secret", isSensitive: true);
        fx.AddValue(key, tenantId: null, environmentId: null, "super-secret-value");

        var safeResult = await fx.Resolver.ResolveAsync(fx.Tenant.TenantCode, fx.Integration.IntegrationName, fx.Environment.Name, revealSecrets: false);
        var runtimeResult = await fx.Resolver.ResolveAsync(fx.Tenant.TenantCode, fx.Integration.IntegrationName, fx.Environment.Name, revealSecrets: true);

        Assert.Equal(ConfigurationResolver.MaskedValue, Assert.Single(safeResult.Items).Value);
        Assert.Equal("super-secret-value", Assert.Single(runtimeResult.Items).Value);
    }

    [Fact]
    public async Task UnknownTenant_ReturnsTenantNotFoundStatus()
    {
        var fx = new ResolverTestFixture();
        fx.AddKey("Some.Key");

        var result = await fx.Resolver.ResolveAsync("does-not-exist", fx.Integration.IntegrationName, fx.Environment.Name);

        Assert.Equal(Core.Dtos.ConfigurationResolveStatus.TenantNotFound, result.Status);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GlobalKey_AppliesRegardlessOfIntegrationType()
    {
        var fx = new ResolverTestFixture();
        var key = new Core.Domain.ConfigurationKey
        {
            Id = Guid.NewGuid(),
            KeyName = "BatchSize",
            DisplayName = "Batch Size",
            DataType = ConfigurationDataType.Int,
            IsEnabled = true,
            IntegrationTypeId = null, // global key
            DefaultValue = "100",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test"
        };
        fx.Db.ConfigurationKeys.Add(key);
        fx.Db.SaveChanges();

        var result = await fx.Resolver.ResolveAsync(fx.Tenant.TenantCode, fx.Integration.IntegrationName, fx.Environment.Name);

        var item = Assert.Single(result.Items);
        Assert.Equal("100", item.Value);
        Assert.Equal(ConfigurationSourceLayer.KeyDefault, item.SourceLayer);
    }
}
