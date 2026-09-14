# HOSCO – Executive AI Dashboard & Smart Alert Chatbot

GD2 backend foundation for a tenant-safe reporting API shared by the future Dashboard and Chatbot. The Chatbot boundary is the Reporting API/query catalog; it never receives direct database access.

## Stack and structure

The repository targets .NET 10 because .NET SDK 10.0.400 is the supported SDK installed during the initial workspace audit. Packages are pinned to patched 10.0.11 releases.

```text
src/
  Hosco.Api/             HTTP, JWT, policies, Swagger, middleware, health
  Hosco.Application/     contracts, scope rules, semantic/query catalogs
  Hosco.Domain/          entities and enums
  Hosco.Infrastructure/  EF Core, SQL Server, seed, reporting/audit stores
tests/
  Hosco.UnitTests/
  Hosco.IntegrationTests/
docs/gd2/
```

## 1. Prerequisites

- .NET SDK 10.0.400 or compatible 10.0.x SDK
- SQL Server, SQL Server Express, container, or LocalDB
- PowerShell examples below; equivalent shell commands also work

## 2. Restore packages and tools

```powershell
dotnet restore Hosco.slnx
dotnet tool restore
```

## 3. Configure database and secrets

The committed connection string and JWT key are development placeholders, not production secrets. Prefer user-secrets:

```powershell
dotnet user-secrets init --project src/Hosco.Api
dotnet user-secrets set "ConnectionStrings:HoscoDb" "Server=localhost;Database=Hosco;Trusted_Connection=True;TrustServerCertificate=True" --project src/Hosco.Api
dotnet user-secrets set "Jwt:SigningKey" "replace-with-at-least-32-random-characters" --project src/Hosco.Api
dotnet user-secrets set "Seed:Enabled" "true" --project src/Hosco.Api
```

Environment variables matching [.env.example](.env.example) are also supported. `.env` is gitignored and is not automatically loaded by the app.

## 4. Apply migration

```powershell
dotnet tool run dotnet-ef database update --project src/Hosco.Infrastructure --startup-project src/Hosco.Api
```

The API also calls `MigrateAsync` at startup. Production deployments should normally run migrations as a controlled release step before starting new instances.

## 5. Seed deterministic demo data

Set `Seed:Enabled=true` or run with the Development profile. On an empty database the startup seed creates two tenants, four branches, six months of orders and anomaly fixtures. A second run is idempotent because it exits once tenant data exists.

```powershell
dotnet run --project src/Hosco.Api --environment Development
```

## 6. Run API and Swagger

```powershell
dotnet run --project src/Hosco.Api --environment Development
```

Open the URL printed by ASP.NET Core and append `/swagger`. Swagger is enabled in Development and can be explicitly controlled by `Swagger:Enabled`.

## 7. Login and get a development JWT

All demo accounts use the development-only password `HoscoDemo!2026`; only a PBKDF2-SHA256 hash is stored in the database.

| Role | Email | Scope |
|---|---|---|
| Owner | `owner@hosco.local` | tenant HOSCO-A |
| Branch Manager | `branch.manager@hosco.local` | branch A-HCM only |
| Chain Manager | `chain.manager@hosco.local` | all HOSCO-A branches |
| System Admin | `admin@hosco.local` | technical admin, tenant HOSCO-A |
| Isolation fixture | `owner@fixture.local` | tenant HOSCO-B |

```powershell
$body = @{ email = "owner@hosco.local"; password = "HoscoDemo!2026" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "http://localhost:5000/api/v1/auth/login" -ContentType "application/json" -Body $body
```

Copy `accessToken` into Swagger's **Authorize** dialog.

## 8. Health checks

```powershell
Invoke-RestMethod http://localhost:5000/health/live
Invoke-RestMethod http://localhost:5000/health/ready
```

`live` does not depend on SQL Server. `ready` calls the database and becomes unhealthy if it is unavailable.

## 9. Run build and tests

```powershell
dotnet build Hosco.slnx
dotnet test Hosco.slnx --no-build
```

Integration tests start the actual HTTP pipeline on an ephemeral local port with an isolated relational SQLite in-memory database. They do not need SQL Server.

## Business-definition status

No approved BA SRS/KPI Dictionary was present in the initial workspace. Metric and query contracts exist, but KPI formulas are marked `BlockedByBusinessDefinition` or `ProvisionalTechnicalPreview`. Do not treat preview aggregates as signed-off business figures; see [GD2 report](docs/gd2/GD2_REPORT.md).
