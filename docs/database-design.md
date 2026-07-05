# Database Design

This document explains the schema in `ConfigurationManager.Data` and the layered override
model implemented by `ConfigurationManager.Core.Services.ConfigurationResolver`.

## Entity overview

| Table | Purpose |
|---|---|
| `Tenants` | A customer/organisation (`TenantCode`, e.g. `acme`). |
| `Environments` | A deployment environment (`Dev`, `UAT`, `Production`). |
| `IntegrationTypes` | The kind of external system being integrated with (`EngagingNetworks`, `Cybertill`, `Shopify`, `Donorfy`). Owns the "Base" layer of configuration keys. |
| `Integrations` | A named integration instance (`IntegrationName`, e.g. `EngagingNetworksSync`) that runtime callers identify themselves with. Belongs to one `IntegrationType`. |
| `ConfigurationSections` / `ConfigurationSubsections` | UI grouping only - no effect on resolution. |
| `ConfigurationKeys` | Metadata for one configuration key: display name, data type, required/sensitive/enabled flags, visibility group, validation rules, help text, and an optional `IntegrationTypeId`. |
| `ConfigurationValues` | The actual override rows - see below. |
| `AuditLogEntries` | Append-only record of changes made through the management API. |

## Why `IntegrationName` resolves to an `IntegrationType`

The runtime resolve API takes `tenantCode`, `integrationName`, and `environmentName` as three
independent parameters. `IntegrationName` identifies a specific integration instance (e.g. "the
Donorfy <-> Engaging Networks sync"); its `IntegrationTypeId` determines which configuration
keys apply to it:

- Keys with `IntegrationTypeId = null` are **global** keys (e.g. `BatchSize`,
  `MaxRetryAttempts`, `EnableDebugLogging`, `TimerSchedule`) and apply to every integration.
- Keys with a non-null `IntegrationTypeId` apply only to integrations of that type (e.g.
  `EngagingNetworks.ClientSecret` only applies to `EngagingNetworks`-type integrations).

This means two integrations of the same type (if you ever create them) share the same base/tenant/
environment overrides for type-scoped keys - the override model does not currently have an
integration-specific layer. That was a deliberate scope decision to match the four layers
described in the brief; if per-integration overrides are needed later, add an `IntegrationId`
column to `ConfigurationValues` and a fifth precedence tier above `TenantEnvironment`.

## The four override layers

`ConfigurationValues` has nullable `TenantId` and `EnvironmentId` columns. Their
null/non-null combination *is* the layer:

| TenantId | EnvironmentId | Layer | Meaning |
|---|---|---|---|
| null | null | **Base** | Default for the integration type (or global, for keys with no type). |
| set | null | **Tenant** | Applies to this tenant, in every environment. |
| null | set | **Environment** | Applies to every tenant, in this one environment. |
| set | set | **TenantEnvironment** | Applies only to this tenant in this environment. |

### Precedence (most specific wins)

```
Base  <  Tenant  <  Environment  <  TenantEnvironment
```

This ordering is as specified in the brief: an environment-wide override (layer 3) beats a
tenant-wide override (layer 2), even though intuitively "tenant" might feel more specific than
"environment". The reasoning: an environment-wide override typically represents an
infrastructure fact ("in Production, always use this endpoint") that should apply even to
tenants who have their own base override, whereas the most specific and highest-precedence
layer remains the combination of both (`TenantEnvironment`). If your organisation wants tenant
overrides to outrank environment overrides, swap the `Tenant`/`Environment` ranks in
`ConfigurationSourceLayer` and `ConfigurationResolver.ResolveKey` - the rest of the system does
not otherwise depend on the exact ordering of those two middle layers.

There is also a fifth, lower-priority fallback used only when **no** `ConfigurationValue` row
exists at all for a key: `ConfigurationKey.DefaultValue` (source layer `KeyDefault`). This is
metadata describing what the key defaults to conceptually (shown in the UI/example), not itself
an override row.

## Disabled-value behaviour (documented design decision)

Both `ConfigurationKey.IsEnabled` and `ConfigurationValue.IsEnabled` exist:

- A **disabled key** is excluded entirely from resolution (the repository only returns enabled
  keys to the resolver in the first place).
- A **disabled value/override** is skipped by the resolver, which then falls back to the next
  less-specific *enabled* layer, continuing down to `Base`, then `KeyDefault`, then `None`.

Example: if the `TenantEnvironment` row for a key is disabled but a `Tenant` row is enabled, the
resolver returns the `Tenant` value with `SourceLayer = Tenant` and `IsEnabled = true` - the
caller sees a normal resolved value, not an error. Only when **no** enabled row (and no
`DefaultValue`) exists anywhere does the item resolve to `SourceLayer = None`, `Value = null`,
`IsEnabled = false`, and (if the key `IsRequired`) `IsMissing = true`. Callers - especially
runtime integration code - should check `IsMissing`/`IsEnabled` rather than assume `Value` is
always populated. This is exercised directly by `ConfigurationResolverTests` (see
`DisabledOverride_FallsBackToNextLessSpecificEnabledLayer` and
`MissingRequiredValue_IsReportedAsMissing`).

## Uniqueness of override rows

There is intended to be at most one `ConfigurationValue` row per
`(ConfigurationKeyId, TenantId, EnvironmentId)` combination. A unique index exists on those three
columns, but **both SQL Server and SQLite treat `NULL` as distinct in unique indexes**, so the
index does not by itself prevent multiple `Base` rows (where both columns are `NULL`) or
multiple rows that share one `NULL` and one matching non-null column. The real guard is
application-level: `ConfigurationValueService.UpsertAsync` always looks up the existing row via
`FindValueAsync(keyId, tenantId, environmentId)` before deciding whether to insert or update.
TODO(production): if moving to Azure SQL where this matters more, consider filtered unique
indexes (`WHERE TenantId IS NOT NULL AND EnvironmentId IS NOT NULL`, etc., one per layer) to get
database-level enforcement of each layer's uniqueness.

## Secrets at rest

Sensitive values are never stored in the `Value` column. Instead:

- `ConfigurationKey.IsSensitive` (always `true` when `DataType = Secret`) tells the system to use
  `ConfigurationValue.EncryptedValue` instead of `Value`.
- `EncryptedValue` holds an AES-256-GCM ciphertext (base64), produced by
  `AesSecretProtector.Protect`. See the README's "How secrets are handled" section for the full
  encryption design and production hardening plan (Azure Key Vault, key rotation, etc.).

## Local dev vs. Azure SQL

The `ConfigurationDbContext` and EF model are provider-agnostic C#; only `Program.cs` (in both
`ConfigurationManager.Api` and any future host) picks the provider, via the `Database:Provider`
setting (`Sqlite` or `SqlServer`) and a connection string. The checked-in EF Core migrations
under `src/ConfigurationManager.Data/Migrations` were generated against SQLite. To move to Azure
SQL:

1. Point `ConnectionStrings:ConfigurationDb` at the Azure SQL database and set
   `Database:Provider=SqlServer`.
2. Regenerate migrations for the SQL Server provider (EF Core migrations are provider-specific):
   run `dotnet ef migrations add InitialCreate -o Migrations/SqlServer` from a copy of the design
   -time factory that calls `UseSqlServer(...)` instead of `UseSqlite(...)`, or maintain two
   migration sets side by side, one per provider.
3. Apply migrations via a controlled deployment step rather than the current
   apply-and-seed-on-startup convenience used for local development (see the `Program.cs` TODO).

## Seed data

`DbInitializer.SeedAsync` (idempotent - checks for existing rows before inserting) creates:

- Environments: `Dev`, `UAT`, `Production`.
- Tenants: `acme` (Acme Charity), `globalgiving` (Global Giving Foundation).
- Integration types: `Donorfy`, `EngagingNetworks`, `Cybertill`, `Shopify`.
- Integrations: `DonorfyCore`, `EngagingNetworksSync`, `CybertillSync`, `ShopifySync`.
- Sections/subsections: `Connection` (`Endpoints`, `Credentials`), `Behaviour` (`Batching`,
  `Scheduling`, `Diagnostics`).
- Keys: `Donorfy.BaseUrl`, `Donorfy.ApiKey`, `EngagingNetworks.ApiBaseUrl`,
  `EngagingNetworks.ClientId`, `EngagingNetworks.ClientSecret`, `BatchSize`,
  `MaxRetryAttempts`, `EnableDebugLogging`, `TimerSchedule`.
- A handful of override rows chosen to exercise every layer and the disabled-fallback path,
  including a deliberately-disabled `TenantEnvironment` override on
  `EngagingNetworks.ClientSecret` for `acme`/`Dev` so it is easy to see the fallback behaviour
  immediately after seeding (it resolves back to the `acme` Tenant-layer secret).

All seeded "secret" values are clearly-labelled placeholders (e.g.
`seed-placeholder-donorfy-api-key`), not real credentials.
