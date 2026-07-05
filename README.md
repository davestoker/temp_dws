# ConfigurationManager

A prototype for managing configuration values used by integrations across different tenants,
customers, integration types, and environments - the kind of thing that backs a Donorfy
Integration Hub style platform, where the same integration code runs for many customers across
Dev/UAT/Production with different endpoints, credentials, and behaviour per customer.

## 1. Purpose

Integration code needs a single place to ask "what is the value of `BatchSize` for tenant `acme`
running `EngagingNetworksSync` in `Dev`?" without every integration re-implementing its own
config file, secrets handling, and override logic. This prototype provides:

- A relational schema for keys, values, and layered overrides.
- Azure Functions APIs for both runtime lookups and management (CRUD).
- A Blazor web UI for browsing and editing configuration.
- Encryption at rest for sensitive values, with a UI that never re-displays a stored secret.
- A simple role model, ready to be swapped for real authentication later.

## 2. Architecture overview

```
src/
  ConfigurationManager.Core   - domain entities, DTOs, ISecretProtector, resolver + management services (no EF/Azure deps)
  ConfigurationManager.Data   - EF Core DbContext, entity configuration, migrations, seed data, EF repository implementation
  ConfigurationManager.Api    - Azure Functions (isolated worker) - thin HTTP endpoints calling Core services
  ConfigurationManager.Web    - Blazor Server UI - calls the Api over HTTP, never touches the database directly
tests/
  ConfigurationManager.Tests  - xUnit tests for the resolver and the AES secret protector
docs/
  database-design.md          - schema + override model in detail
  ConfigurationManager.http   - example requests for every endpoint
```

Design principle: **API endpoints are thin** (auth check, parse request, call a Core service,
shape the response). **Business rules live in `ConfigurationManager.Core.Services`**
(`ConfigurationResolver`, `ConfigurationKeyService`, `ConfigurationValueService`). **Database
access is isolated** behind `IConfigurationRepository` / `IConfigurationAdminRepository`, so the
resolver can be (and is) unit tested without EF Core, Azure Functions, or the web UI. **Encryption
is isolated** behind `ISecretProtector`, implemented by `AesSecretProtector`.

The Web project never talks to the database - it calls the Api's Functions over HTTP, exactly as
a production deployment would (Web and Api could scale, deploy, and be secured independently).

## 3. How configuration layering works

Configuration values are resolved from four layers, most specific wins:

```
Base  <  Tenant  <  Environment  <  TenantEnvironment
```

- **Base** - default for an `IntegrationType` (e.g. all `EngagingNetworks` integrations).
- **Tenant** - overridden for one tenant, across all environments.
- **Environment** - overridden for one environment, across all tenants.
- **TenantEnvironment** - overridden for one tenant in one specific environment (most specific).

If the most specific matching row is **disabled**, the resolver falls back to the next
less-specific *enabled* row (Base, then the key's metadata `DefaultValue`), rather than failing.
If nothing enabled is found anywhere and the key is `IsRequired`, the resolved item comes back
with `IsMissing = true` and `Value = null` instead of throwing - callers must check this flag.

Full rationale, the exact precedence table, and why "Environment" outranks "Tenant" per the
brief's original ordering, is in [`docs/database-design.md`](docs/database-design.md).

## 4. Security model

- **AuthN/AuthZ boundary**: every Functions endpoint requires an `X-Api-Key` header, checked by
  `ConfigurationManager.Api.Auth.ApiKeyAuthenticator` against a configured key-to-role map
  (`ApiKeys` section in `local.settings.json`). This is intentionally simple so it is easy to
  reason about, and isolated so it can be swapped for real auth without touching function
  signatures - see `AuthContext`.
- **Roles**: `Admin`, `ITSupport`, `Owner`, `RuntimeReader` (`ConfigurationManager.Core.Enums.AppRole`).
  - `Admin` - full read/write, including key metadata.
  - `ITSupport` - can view/edit configuration values, cannot manage key metadata.
  - `Owner` - can view/edit values only for keys whose `VisibilityGroup = Owner`.
  - `RuntimeReader` - the only role (besides `Admin`) allowed to call the decrypted runtime
    resolve endpoint; intended for the integration service identity, not a human.
- **Two separate resolve endpoints** rather than one endpoint with a "reveal secrets" flag, so
  there is no query-string toggle an unauthorised caller could flip:
  - `GET /api/configuration/resolve` - safe/UI mode, sensitive values always masked as `********`.
  - `GET /api/runtime/configuration/resolve` - decrypted mode, restricted to `RuntimeReader`/`Admin`.
- **No secrets in logs or generic responses**: error responses only ever return a short generic
  message (`FunctionBase.ErrorAsync`); nothing logs raw secret values; `AuditLogger.Redacted`
  forces sensitive fields to `<redacted>` before they reach the audit log.
- **Input validation**: request DTOs are validated in the Core services (key name uniqueness,
  required fields, data-type-appropriate value parsing for `Int`/`Decimal`/`Boolean`/`DateTime`/`Json`).
- **Parameterised/EF-safe queries**: all database access goes through EF Core LINQ - no
  string-concatenated SQL anywhere in the codebase.
- **Audit fields** (`CreatedAt`/`CreatedBy`/`UpdatedAt`/`UpdatedBy`) on every editable entity, plus
  an append-only `AuditLogEntries` table recording key/value creation, updates, and enable/disable
  actions.

### TODOs for production (all called out in code comments too)

| Area | Prototype | Production TODO |
|---|---|---|
| AuthN | Static API key -> role map | Microsoft Entra ID (Azure AD), validate bearer tokens via `Microsoft.Identity.Web`, map app roles/groups to `AppRole` |
| Runtime identity | Shared `RuntimeReader` API key | Managed Identity for the calling Function/App Service |
| Secrets | Local AES-256-GCM key from config | Azure Key Vault-backed key, accessed via Managed Identity, with rotation/versioning |
| Audit logging | Basic table, no tamper-evidence | Immutable/append-only store (e.g. Azure Table Storage with SAS restrictions, or a write-once log sink); ship to a SIEM |
| Rate limiting | None | APIM or Functions-level rate limiting, especially on the runtime resolve endpoint |
| Change approval | None - any Admin can edit Production values immediately | Approval workflow for Production-environment changes (e.g. a review step before a `TenantEnvironment`/`Environment` override targeting `Production` takes effect) |
| Migrations | Applied automatically on Functions startup | Run via a controlled release pipeline step, not on every cold start |

## 5. How secrets are handled

1. A key is sensitive if `ConfigurationKey.IsSensitive = true` (always true when
   `DataType = Secret`).
2. When a sensitive value is saved, `ConfigurationValueService` calls
   `ISecretProtector.Protect(plainText)` and stores the result in
   `ConfigurationValue.EncryptedValue`; the plain `Value` column stays `null`.
3. `AesSecretProtector` implements `ISecretProtector` using AES-256-GCM with a random 12-byte
   nonce per call (so encrypting the same value twice yields different ciphertext) and a 16-byte
   authentication tag; the payload is `base64(nonce || tag || ciphertext)`. The key comes from
   `Secrets:EncryptionKeyBase64` (generate with `openssl rand -base64 32`) - in production this
   should move to Azure Key Vault (see TODO table above).
4. **Listing/UI endpoints and the safe resolve endpoint never return the decrypted value** - they
   return `********` (see `ConfigurationResolver.MaskedValue`) whenever a value exists, or
   nothing if it doesn't.
5. **Replacing a secret**: the management API's upsert endpoint accepts a plain `value`; if
   provided, it replaces the stored ciphertext. If the field is left blank/omitted while just
   toggling `isEnabled`, the existing secret is left untouched (see
   `ConfigurationValueService.ApplyValue`).
6. **The only way to get a decrypted value back** is the `RuntimeReader`/`Admin`-only
   `/api/runtime/configuration/resolve` endpoint, intended for integration code, not for humans
   browsing the UI.

## 6. How to run locally

Prerequisites: .NET 8 SDK, and (only if you want to actually invoke the Functions endpoints)
[Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local).
No SQL Server install needed - local dev uses a SQLite file created automatically.

```bash
# 1. Restore/build everything
dotnet build ConfigurationManager.sln

# 2. Configure the Api project
cd src/ConfigurationManager.Api
cp local.settings.json.example local.settings.json
# Generate a real encryption key and put it in local.settings.json:
openssl rand -base64 32
# paste the output into "Secrets:EncryptionKeyBase64"

# 3. Run the API (applies EF Core migrations and seeds example data automatically on startup)
func start

# 4. In a second terminal, run the web UI
cd src/ConfigurationManager.Web
dotnet run
# Browse to the URL printed in the console (e.g. https://localhost:7183), then go to /login
# and pick a role (Admin / ITSupport / Owner / RuntimeReader) - these map to the same
# dev API keys configured in the Api project's local.settings.json.
```

Run the tests:

```bash
dotnet test tests/ConfigurationManager.Tests/ConfigurationManager.Tests.csproj
```

Example requests for every endpoint (safe to run against a freshly-seeded database) are in
[`docs/ConfigurationManager.http`](docs/ConfigurationManager.http).

> **Note on this sandbox**: this repository was built in a sandboxed environment whose network
> policy blocks the Azure Functions Core Tools download (`cdn.functions.azure.com`), so `func
> start` could not be exercised here. Everything was verified another way: `dotnet build` on the
> full solution, `dotnet test` (15/15 passing), running the Blazor Web UI directly with `dotnet
> run` (confirmed the login/explorer pages render), and a standalone harness that ran the real EF
> Core migrations + `DbInitializer` seeding + `ConfigurationResolver` against a SQLite file,
> confirming layered resolution, the disabled-override fallback, and secret encrypt/decrypt all
> behave as documented. On a normal developer machine with unrestricted internet access, `func
> start` should work as described above.

## 7. How to create/update configuration

- **Create a key**: `POST /api/configuration/keys` (Admin only) - see the `.http` file for a full
  example, or use the "Create Key" form on the `/keys` page in the web UI.
- **Set/override a value**: `POST /api/configuration/values` with `configurationKeyId` and
  optionally `tenantCode`/`environmentName` to target a specific layer (omit both for Base, set
  one for Tenant/Environment, set both for TenantEnvironment). The web UI's "Edit" button on the
  Configuration Explorer page always targets the `TenantEnvironment` layer for whichever
  tenant/environment you have selected.
- **Enable/disable** a key or value: `POST /api/configuration/keys/{id}/enable|disable` or
  `POST /api/configuration/values/{id}/enable|disable`.
- Every change is validated (data type, required fields) and recorded in `AuditLogEntries`.

## 8. How to call the runtime resolve endpoint

```
GET /api/runtime/configuration/resolve?tenantCode=acme&integrationName=EngagingNetworksSync&environmentName=Dev
X-Api-Key: <a RuntimeReader or Admin key>
```

Returns, for every applicable enabled key:

```json
{
  "status": "Ok",
  "items": [
    {
      "configurationKeyId": "...",
      "keyName": "EngagingNetworks.ClientSecret",
      "displayName": "Engaging Networks Client Secret",
      "dataType": "Secret",
      "value": "the-decrypted-secret",
      "sourceLayer": "Tenant",
      "isSensitive": true,
      "isEnabled": true,
      "isRequired": true,
      "isMissing": false
    }
  ]
}
```

Integration code should treat `isMissing: true` as a hard failure for required keys, and should
never log the `value` field when `isSensitive: true`.

## 9. Known prototype limitations

- Authentication is a static API-key-to-role map, not real identity - fine for a prototype, not
  for production (see the TODO table above).
- No per-integration override layer - keys scoped to an `IntegrationType` apply to *every*
  `Integration` of that type, not to one specific named integration. See
  `docs/database-design.md` for how to extend this if needed.
- The unique index intended to prevent duplicate override rows does not fully enforce uniqueness
  at the database level for `NULL` tenant/environment combinations (SQL Server/SQLite both treat
  `NULL` as distinct in unique indexes) - the application layer is the real guard. See
  `docs/database-design.md`.
- No approval workflow, rate limiting, or tamper-evident audit log yet.
- `Owner`-role visibility-group enforcement is a single check on write (values only); it is not a
  full row-level security model.
- Migrations are applied automatically on every Functions cold start, which is convenient for a
  prototype but not something you want in a real deployment pipeline.
- The web UI is deliberately plain (Bootstrap defaults, no custom styling polish).
- `func start` was not exercised in the environment this was built in (see the network note in
  section 6) - the API was instead verified via `dotnet build`, `dotnet test`, and a standalone
  harness exercising the same EF Core + resolver code paths.

## 10. Suggested production hardening steps

See the TODO table in section 4, plus:

- Move `Secrets:EncryptionKeyBase64` and all `ApiKeys` entries out of configuration files and into
  Azure Key Vault / Managed Identity.
- Add integration tests that actually run `func start` and hit the HTTP endpoints (not just the
  Core services), once running in an environment with normal internet access.
- Add response caching/ETags to the runtime resolve endpoint if it becomes hot-path for many
  integration calls.
- Consider soft-delete instead of hard delete everywhere (there is currently no delete endpoint at
  all - keys/values can only be disabled, which is itself a reasonable prototype choice worth
  keeping).
