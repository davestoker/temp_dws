using ConfigurationManager.Core.Domain;
using ConfigurationManager.Core.Enums;
using ConfigurationManager.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConfigurationManager.Data.Seeding;

/// <summary>
/// Applies pending migrations and seeds example reference data + configuration keys/values for
/// local development and demos. Safe to call every startup - it is idempotent (checks for
/// existing rows before inserting). Secrets are encrypted via <see cref="ISecretProtector"/>
/// before being written, and the seeded "secret" values here are placeholders, not real
/// credentials.
/// </summary>
public static class DbInitializer
{
    private const string SeedActor = "seed-data";

    public static async Task SeedAsync(ConfigurationDbContext db, ISecretProtector secretProtector, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        var now = DateTimeOffset.UtcNow;

        var environments = await EnsureEnvironmentsAsync(db, now, ct);
        var tenants = await EnsureTenantsAsync(db, now, ct);
        var integrationTypes = await EnsureIntegrationTypesAsync(db, now, ct);
        var integrations = await EnsureIntegrationsAsync(db, integrationTypes, now, ct);
        var (sections, subsections) = await EnsureSectionsAsync(db, now, ct);
        var keys = await EnsureKeysAsync(db, integrationTypes, sections, subsections, now, ct);
        await EnsureValuesAsync(db, keys, tenants, environments, secretProtector, now, ct);
    }

    private static async Task<Dictionary<string, EnvironmentDefinition>> EnsureEnvironmentsAsync(
        ConfigurationDbContext db, DateTimeOffset now, CancellationToken ct)
    {
        var wanted = new[] { ("Dev", 1), ("UAT", 2), ("Production", 3) };
        foreach (var (name, sortOrder) in wanted)
        {
            if (!await db.Environments.AnyAsync(e => e.Name == name, ct))
            {
                db.Environments.Add(new EnvironmentDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    SortOrder = sortOrder,
                    IsEnabled = true,
                    CreatedAt = now,
                    CreatedBy = SeedActor
                });
            }
        }
        await db.SaveChangesAsync(ct);
        return await db.Environments.ToDictionaryAsync(e => e.Name, ct);
    }

    private static async Task<Dictionary<string, Tenant>> EnsureTenantsAsync(
        ConfigurationDbContext db, DateTimeOffset now, CancellationToken ct)
    {
        var wanted = new[]
        {
            ("acme", "Acme Charity"),
            ("globalgiving", "Global Giving Foundation")
        };

        foreach (var (code, name) in wanted)
        {
            if (!await db.Tenants.AnyAsync(t => t.TenantCode == code, ct))
            {
                db.Tenants.Add(new Tenant
                {
                    Id = Guid.NewGuid(),
                    TenantCode = code,
                    Name = name,
                    IsEnabled = true,
                    CreatedAt = now,
                    CreatedBy = SeedActor
                });
            }
        }
        await db.SaveChangesAsync(ct);
        return await db.Tenants.ToDictionaryAsync(t => t.TenantCode, ct);
    }

    private static async Task<Dictionary<string, IntegrationType>> EnsureIntegrationTypesAsync(
        ConfigurationDbContext db, DateTimeOffset now, CancellationToken ct)
    {
        var wanted = new[]
        {
            ("Donorfy", "Donorfy Integration Hub core platform settings"),
            ("EngagingNetworks", "Engaging Networks CRM integration"),
            ("Cybertill", "Cybertill EPOS/retail integration"),
            ("Shopify", "Shopify e-commerce integration")
        };

        foreach (var (name, description) in wanted)
        {
            if (!await db.IntegrationTypes.AnyAsync(i => i.Name == name, ct))
            {
                db.IntegrationTypes.Add(new IntegrationType
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = description,
                    IsEnabled = true,
                    CreatedAt = now,
                    CreatedBy = SeedActor
                });
            }
        }
        await db.SaveChangesAsync(ct);
        return await db.IntegrationTypes.ToDictionaryAsync(i => i.Name, ct);
    }

    private static async Task<Dictionary<string, Integration>> EnsureIntegrationsAsync(
        ConfigurationDbContext db, Dictionary<string, IntegrationType> integrationTypes, DateTimeOffset now, CancellationToken ct)
    {
        var wanted = new[]
        {
            ("DonorfyCore", "Donorfy", "Core Donorfy platform configuration"),
            ("EngagingNetworksSync", "EngagingNetworks", "Donorfy <-> Engaging Networks donor sync"),
            ("CybertillSync", "Cybertill", "Donorfy <-> Cybertill retail sync"),
            ("ShopifySync", "Shopify", "Donorfy <-> Shopify e-commerce sync")
        };

        foreach (var (integrationName, typeName, description) in wanted)
        {
            if (!await db.Integrations.AnyAsync(i => i.IntegrationName == integrationName, ct))
            {
                db.Integrations.Add(new Integration
                {
                    Id = Guid.NewGuid(),
                    IntegrationName = integrationName,
                    IntegrationTypeId = integrationTypes[typeName].Id,
                    Description = description,
                    IsEnabled = true,
                    CreatedAt = now,
                    CreatedBy = SeedActor
                });
            }
        }
        await db.SaveChangesAsync(ct);
        return await db.Integrations.ToDictionaryAsync(i => i.IntegrationName, ct);
    }

    private static async Task<(Dictionary<string, ConfigurationSection> Sections, Dictionary<string, ConfigurationSubsection> Subsections)> EnsureSectionsAsync(
        ConfigurationDbContext db, DateTimeOffset now, CancellationToken ct)
    {
        if (!await db.ConfigurationSections.AnyAsync(ct))
        {
            var connectionSection = new ConfigurationSection { Id = Guid.NewGuid(), Name = "Connection", SortOrder = 1, CreatedAt = now, CreatedBy = SeedActor };
            var behaviourSection = new ConfigurationSection { Id = Guid.NewGuid(), Name = "Behaviour", SortOrder = 2, CreatedAt = now, CreatedBy = SeedActor };

            db.ConfigurationSections.AddRange(connectionSection, behaviourSection);
            await db.SaveChangesAsync(ct);

            db.ConfigurationSubsections.AddRange(
                new ConfigurationSubsection { Id = Guid.NewGuid(), SectionId = connectionSection.Id, Name = "Endpoints", SortOrder = 1, CreatedAt = now, CreatedBy = SeedActor },
                new ConfigurationSubsection { Id = Guid.NewGuid(), SectionId = connectionSection.Id, Name = "Credentials", SortOrder = 2, CreatedAt = now, CreatedBy = SeedActor },
                new ConfigurationSubsection { Id = Guid.NewGuid(), SectionId = behaviourSection.Id, Name = "Batching", SortOrder = 1, CreatedAt = now, CreatedBy = SeedActor },
                new ConfigurationSubsection { Id = Guid.NewGuid(), SectionId = behaviourSection.Id, Name = "Scheduling", SortOrder = 2, CreatedAt = now, CreatedBy = SeedActor },
                new ConfigurationSubsection { Id = Guid.NewGuid(), SectionId = behaviourSection.Id, Name = "Diagnostics", SortOrder = 3, CreatedAt = now, CreatedBy = SeedActor });
            await db.SaveChangesAsync(ct);
        }

        var sections = await db.ConfigurationSections.ToDictionaryAsync(s => s.Name, ct);
        var subsections = await db.ConfigurationSubsections.ToDictionaryAsync(s => s.Name, ct);
        return (sections, subsections);
    }

    private static async Task<Dictionary<string, ConfigurationKey>> EnsureKeysAsync(
        ConfigurationDbContext db,
        Dictionary<string, IntegrationType> integrationTypes,
        Dictionary<string, ConfigurationSection> sections,
        Dictionary<string, ConfigurationSubsection> subsections,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var definitions = new List<ConfigurationKey>
        {
            new()
            {
                KeyName = "Donorfy.BaseUrl", DisplayName = "Donorfy Base URL",
                Description = "Root URL of the Donorfy API for this integration.",
                DataType = ConfigurationDataType.String, DefaultValue = "https://api.donorfy.com",
                IsRequired = true, IsSensitive = false, VisibilityGroup = VisibilityGroup.ITSupport,
                IntegrationTypeId = integrationTypes["Donorfy"].Id, SectionId = sections["Connection"].Id, SubsectionId = subsections["Endpoints"].Id,
                SortOrder = 1, ExampleValue = "https://api.donorfy.com", HelpText = "Do not include a trailing slash."
            },
            new()
            {
                KeyName = "Donorfy.ApiKey", DisplayName = "Donorfy API Key",
                Description = "API key used to authenticate against the Donorfy API.",
                DataType = ConfigurationDataType.Secret, IsRequired = true, IsSensitive = true,
                VisibilityGroup = VisibilityGroup.Admin,
                IntegrationTypeId = integrationTypes["Donorfy"].Id, SectionId = sections["Connection"].Id, SubsectionId = subsections["Credentials"].Id,
                SortOrder = 2, HelpText = "Rotate via the Donorfy admin portal; replacing this value here does not rotate it in Donorfy."
            },
            new()
            {
                KeyName = "EngagingNetworks.ApiBaseUrl", DisplayName = "Engaging Networks API Base URL",
                Description = "Root URL of the Engaging Networks API.",
                DataType = ConfigurationDataType.String, DefaultValue = "https://api.engagingnetworks.app",
                IsRequired = true, IsSensitive = false, VisibilityGroup = VisibilityGroup.ITSupport,
                IntegrationTypeId = integrationTypes["EngagingNetworks"].Id, SectionId = sections["Connection"].Id, SubsectionId = subsections["Endpoints"].Id,
                SortOrder = 1, ExampleValue = "https://api.engagingnetworks.app"
            },
            new()
            {
                KeyName = "EngagingNetworks.ClientId", DisplayName = "Engaging Networks Client ID",
                Description = "OAuth client id for the Engaging Networks integration.",
                DataType = ConfigurationDataType.String, IsRequired = true, IsSensitive = false,
                VisibilityGroup = VisibilityGroup.ITSupport,
                IntegrationTypeId = integrationTypes["EngagingNetworks"].Id, SectionId = sections["Connection"].Id, SubsectionId = subsections["Credentials"].Id,
                SortOrder = 2
            },
            new()
            {
                KeyName = "EngagingNetworks.ClientSecret", DisplayName = "Engaging Networks Client Secret",
                Description = "OAuth client secret for the Engaging Networks integration.",
                DataType = ConfigurationDataType.Secret, IsRequired = true, IsSensitive = true,
                VisibilityGroup = VisibilityGroup.Admin,
                IntegrationTypeId = integrationTypes["EngagingNetworks"].Id, SectionId = sections["Connection"].Id, SubsectionId = subsections["Credentials"].Id,
                SortOrder = 3
            },
            new()
            {
                KeyName = "BatchSize", DisplayName = "Batch Size",
                Description = "Number of records to process per sync batch.",
                DataType = ConfigurationDataType.Int, DefaultValue = "100",
                IsRequired = true, IsSensitive = false, VisibilityGroup = VisibilityGroup.ITSupport,
                IntegrationTypeId = null, SectionId = sections["Behaviour"].Id, SubsectionId = subsections["Batching"].Id,
                SortOrder = 1, ValidationRules = "min:1,max:5000", ExampleValue = "100"
            },
            new()
            {
                KeyName = "MaxRetryAttempts", DisplayName = "Max Retry Attempts",
                Description = "Number of times a failed operation is retried before giving up.",
                DataType = ConfigurationDataType.Int, DefaultValue = "3",
                IsRequired = true, IsSensitive = false, VisibilityGroup = VisibilityGroup.ITSupport,
                IntegrationTypeId = null, SectionId = sections["Behaviour"].Id, SubsectionId = subsections["Batching"].Id,
                SortOrder = 2, ValidationRules = "min:0,max:10", ExampleValue = "3"
            },
            new()
            {
                KeyName = "EnableDebugLogging", DisplayName = "Enable Debug Logging",
                Description = "Turns on verbose diagnostic logging for troubleshooting. Never logs secret values regardless of this setting.",
                DataType = ConfigurationDataType.Boolean, DefaultValue = "false",
                IsRequired = false, IsSensitive = false, VisibilityGroup = VisibilityGroup.ITSupport,
                IntegrationTypeId = null, SectionId = sections["Behaviour"].Id, SubsectionId = subsections["Diagnostics"].Id,
                SortOrder = 3, ExampleValue = "false"
            },
            new()
            {
                KeyName = "TimerSchedule", DisplayName = "Timer Schedule (CRON)",
                Description = "NCRONTAB expression controlling how often the sync timer trigger runs.",
                DataType = ConfigurationDataType.String, DefaultValue = "0 */15 * * * *",
                IsRequired = true, IsSensitive = false, VisibilityGroup = VisibilityGroup.Owner,
                IntegrationTypeId = null, SectionId = sections["Behaviour"].Id, SubsectionId = subsections["Scheduling"].Id,
                SortOrder = 4, ExampleValue = "0 */15 * * * *", HelpText = "Every 15 minutes by default."
            }
        };

        foreach (var key in definitions)
        {
            if (!await db.ConfigurationKeys.AnyAsync(k => k.KeyName == key.KeyName, ct))
            {
                key.Id = Guid.NewGuid();
                key.IsEnabled = true;
                key.CreatedAt = now;
                key.CreatedBy = SeedActor;
                db.ConfigurationKeys.Add(key);
            }
        }
        await db.SaveChangesAsync(ct);
        return await db.ConfigurationKeys.ToDictionaryAsync(k => k.KeyName, ct);
    }

    private static async Task EnsureValuesAsync(
        ConfigurationDbContext db,
        Dictionary<string, ConfigurationKey> keys,
        Dictionary<string, Tenant> tenants,
        Dictionary<string, EnvironmentDefinition> environments,
        ISecretProtector secretProtector,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (await db.ConfigurationValues.AnyAsync(ct))
        {
            return;
        }

        var acme = tenants["acme"];
        var globalGiving = tenants["globalgiving"];
        var dev = environments["Dev"];
        var uat = environments["UAT"];
        var production = environments["Production"];

        void AddValue(string keyName, Guid? tenantId, Guid? environmentId, string? plainValue, bool isEnabled = true)
        {
            var key = keys[keyName];
            var value = new ConfigurationValue
            {
                Id = Guid.NewGuid(),
                ConfigurationKeyId = key.Id,
                TenantId = tenantId,
                EnvironmentId = environmentId,
                IsEnabled = isEnabled,
                CreatedAt = now,
                CreatedBy = SeedActor
            };

            if (key.IsSensitive)
            {
                value.EncryptedValue = plainValue is null ? null : secretProtector.Protect(plainValue);
            }
            else
            {
                value.Value = plainValue;
            }

            db.ConfigurationValues.Add(value);
        }

        // Donorfy: Base URL + tenant override for globalgiving; API key base value (placeholder).
        AddValue("Donorfy.BaseUrl", null, null, "https://api.donorfy.com");
        AddValue("Donorfy.BaseUrl", globalGiving.Id, null, "https://api.globalgiving.donorfy.com");
        AddValue("Donorfy.ApiKey", null, null, "seed-placeholder-donorfy-api-key");

        // EngagingNetworks: base config, tenant override, and a Production-environment-wide override
        // demonstrating the Environment layer (applies to all tenants in Production).
        AddValue("EngagingNetworks.ApiBaseUrl", null, null, "https://api.engagingnetworks.app");
        AddValue("EngagingNetworks.ApiBaseUrl", null, production.Id, "https://api.engagingnetworks.app/v2");
        AddValue("EngagingNetworks.ClientId", acme.Id, null, "acme-en-client-id");
        AddValue("EngagingNetworks.ClientId", acme.Id, uat.Id, "acme-en-client-id-uat");
        AddValue("EngagingNetworks.ClientSecret", acme.Id, null, "seed-placeholder-secret-base");
        // Tenant+Environment override for acme/Dev, deliberately disabled to demonstrate fallback.
        AddValue("EngagingNetworks.ClientSecret", acme.Id, dev.Id, "seed-placeholder-secret-dev-disabled", isEnabled: false);

        // Global behaviour keys with a tenant-specific batch size override.
        AddValue("BatchSize", globalGiving.Id, null, "250");
        AddValue("MaxRetryAttempts", null, production.Id, "5");
        AddValue("EnableDebugLogging", null, dev.Id, "true");
        AddValue("EnableDebugLogging", null, production.Id, "false");
        AddValue("TimerSchedule", acme.Id, production.Id, "0 0 * * * *");

        await db.SaveChangesAsync(ct);
    }
}
