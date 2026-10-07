# HOSCO — Tester setup (Windows PowerShell)

Branch: `feature/gd4-ai-chatbot`. Handoff baseline: `835829895f1329b1ca53bf6ee9d4102c8ac6af63`.
Read [handoff validation](HANDOFF_REPORT.md), [checklist](TESTER_CHECKLIST.md) and [AI smoke tests](LIVE_AI_SMOKE_TEST.md).
Mock is the default. No API key, paid LLM call, Telegram token or SQL Server installation is needed for the quick-start.

## 1. Prerequisites

- Windows with an approved PowerShell 7 terminal for the validated scripts, or PowerShell 5.1 for the manual commands, Git and network access to NuGet/npm for first restore. PowerShell 5.1 script startup on the audited machine was blocked by execution policy; it is not marked runtime PASS.
- .NET **SDK 10.0.400** (not just a runtime). `global.json` uses `latestPatch`, so another SDK feature band is not assumed compatible. Packages target .NET/ASP.NET Core 10.0.11.
- Node.js matching Vite's installed requirement: `^20.19.0 || >=22.12.0`; npm included. Audited machine: Node 26.7.0, npm 11.19.0.
- Optional: SQL Server Express LocalDB / SQL Server for **persistent** testing. Use a dedicated disposable tester database, never production or somebody else's development DB.
- Enterprise Windows may block unsigned local assemblies. Obtain a policy-approved trusted environment from IT; do not disable WDAC/Code Integrity or reinstall .NET as a workaround.

```powershell
git --version
dotnet --info
dotnet --version
node --version
npm.cmd --version
```

Scripts do not change PowerShell execution policy. If company policy blocks `.ps1`, use the manual commands below in an approved terminal or ask IT. Prefer `npm.cmd` to avoid the unrelated `npm.ps1` policy problem.

## 2. Clone and select the existing branch

For a **new clone only** (do not switch/reset an existing dirty workspace):

```powershell
git clone --branch feature/gd4-ai-chatbot --single-branch https://github.com/PhanPhong13112005/hosco-executive-ai-dashboard.git
Set-Location .\hosco-executive-ai-dashboard
git branch --show-current
git rev-parse HEAD
git status --short
```

For an existing clean tester clone, `git switch feature/gd4-ai-chatbot` selects the branch. This is an instruction for the Tester, not an operation performed on the current development workspace.
These scripts, handoff documents and BUG-UI-001 regression files are delivered together on this feature branch. Confirm the developer's published revision before testing; `8358298` is the pre-handoff baseline, not the complete tester revision. After publication, a new clone should contain `scripts/setup-tester.ps1` and `docs/testing/TESTER_SETUP.md`. Verify `git status` is clean and `git branch -vv` shows the intended upstream. If publication is pending or a different revision was cloned, request the complete reviewed delivery; do not assume missing files are an application defect.

## 3. Quick setup: restore, tools, backend build, npm install/build

Run from repo root:

```powershell
.\scripts\setup-tester.ps1
```

It checks tools, restores packages and local EF tooling, builds the solution, uses lockfile-based `npm.cmd ci --no-audit --no-fund`, and builds the frontend. `npm ci` replaces installed dependencies, not source or the lockfile. It does **not** migrate any database, obtain secrets, run live AI, upgrade dependencies, change Git or Windows policies.
For prerequisites only: `.\scripts\setup-tester.ps1 -CheckOnly`.

Manual equivalent:

```powershell
dotnet restore Hosco.slnx
dotnet tool restore --tool-manifest dotnet-tools.json
dotnet build Hosco.slnx --no-restore
Push-Location src\Hosco.Web
try {
    npm.cmd ci --no-audit --no-fund
    npm.cmd run build
} finally { Pop-Location }
```

Stop after any failing manual command; do not treat the later command's exit code as the earlier result.

## 4. Database, migrations and seed

### Quick-start: SQLite in-memory (default run script)

No DB server, connection string or `dotnet-ef database update` is needed. Existing API startup calls `EnsureCreatedAsync` for SQLite then `DemoSeed.SeedAsync`. This is a relational **ephemeral** test mode, not a migration or SQL Server compatibility check. It does not write a SQLite file and cannot verify restart persistence. All rule edits, alerts, acknowledgements, delivery records and audit rows vanish when the backend stops.

The script pins `Seed:Enabled=true` and Telegram disabled; scheduler is off unless `-EnableScheduler` is supplied. Current seed has two tenants, four branches, five demo users and historical sales/anomaly fixtures in **January–June 2026**. For meaningful sales/export checks choose a period in that range; questions about today/this week can correctly return zero with this historical seed.

### Persistent test database: SQL Server / LocalDB

Install/initialize LocalDB through the normal approved SQL Server setup. In the **backend terminal**, use a fresh dedicated DB name:

```powershell
$env:ConnectionStrings__HoscoDb = 'Server=(localdb)\MSSQLLocalDB;Database=HoscoTester;Trusted_Connection=True;TrustServerCertificate=True'
dotnet tool restore --tool-manifest dotnet-tools.json
dotnet tool run dotnet-ef database update --project src/Hosco.Infrastructure --startup-project src/Hosco.Api
.\scripts\run-tester.ps1 -Service Backend -Database SqlServer -AiProvider Mock
```

The design-time factory reads **`ConnectionStrings__HoscoDb` from environment**, not an arbitrary appsettings/user-secrets value. API SQL Server startup also calls `MigrateAsync`, then seed when enabled. Expected migrations:

- `20260914161316_InitialCreate`
- `20260915151000_AddDashboardAlertFoundation`
- `20260918161734_SyncFinalGd1BusinessRules`
- `20260929062905_CompleteGd3NotificationDelivery`

Seed on an existing tenant database calls `EnsureFinalBusinessFixturesAsync` (it is not a no-op). Use a dedicated DB; do not point this script at production or existing custom rule configurations. Never delete/reseed a database to hide a defect. SQL Server migration/startup in this handoff is documented; see the report for what was actually runtime-validated.

## 5. Run backend and frontend (two terminals)

Terminal 1, repo root:

```powershell
.\scripts\run-tester.ps1 -Service Backend -AiProvider Mock
```

Terminal 2, same repo root:

```powershell
.\scripts\run-tester.ps1 -Service Frontend
```

Open **http://localhost:5173/**. API **http://localhost:5000/**, Swagger **http://localhost:5000/swagger**, readiness **http://localhost:5000/health/ready**. Scripts use strict frontend port selection, do not kill other processes and run in the foreground; stop each with Ctrl+C. If either port is occupied, stop only your own previous server or coordinate with its owner.

The existing launch profile defaults to **5179**, whereas Vite's existing proxy targets **5000**. The script deliberately uses `--no-launch-profile` and explicit URL 5000. Do not mix a plain launch-profile startup with the proxy quick-start.

Manual backend equivalent (Mock, ephemeral SQLite):

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:AI_PROVIDER = 'Mock'
$env:AI_MODEL = ''
# Generate a backend-only key without printing or writing it.
$jwtBytes = New-Object byte[] 48
$jwtRng = [Security.Cryptography.RandomNumberGenerator]::Create()
try { $jwtRng.GetBytes($jwtBytes) } finally { $jwtRng.Dispose() }
$env:Jwt__SigningKey = [Convert]::ToBase64String($jwtBytes)
dotnet run --project src/Hosco.Api --no-build --no-launch-profile -- --urls=http://localhost:5000 --Database:Provider=Sqlite --Seed:Enabled=true --Swagger:Enabled=true --AlertScheduler:Enabled=false --Notifications:Telegram:Enabled=false
```

Manual frontend equivalent in a **separate terminal without backend secrets**:

```powershell
Set-Location src\Hosco.Web
$env:VITE_API_BASE_URL = ''
npm.cmd run dev -- --host 127.0.0.1 --port 5173 --strictPort
```

Empty `VITE_API_BASE_URL` uses relative `/api` and the existing Vite proxy, so no custom CORS change is required. If running frontend/API on separate origins without the proxy, set `VITE_API_BASE_URL` and matching `Cors__AllowedOrigins__0` explicitly and rebuild/restart the frontend. Never prefix provider credentials with `VITE_`.

## 6. Configuration and JWT

| Setting | Tester mechanism |
|---|---|
| `AI_PROVIDER` | `-AiProvider Mock`, `Gemini` or `OpenAI`; script pins selection, default Mock even if a live provider was inherited |
| `AI_MODEL` | Backend process environment, explicit supported model identifier; no default live model |
| `OPENAI_API_KEY`, `GEMINI_API_KEY` | Backend process environment only; empty examples, never command-line arguments |
| `AI_TIMEOUT_SECONDS`, `AI_MAX_RETRIES` | Script pins existing defaults 5 seconds and 1 retry; manual host accepts ranges 1–10 and 0–1 |
| `Database:Provider` | Script `-Database Sqlite` or `SqlServer` |
| `ConnectionStrings__HoscoDb` | Required environment value for SqlServer; never displayed by the script |
| `Seed:Enabled` | Script pins true; development fixtures only |
| `Jwt__SigningKey` | Script generates an ephemeral random key if absent, or accepts an explicitly provided key of at least 32 characters |
| `Jwt__Issuer`, `Jwt__Audience` | Existing `Hosco.Api`, `Hosco.Clients`; keep aligned between token issuer and validator |
| `AlertScheduler:Enabled` | Off by default; use `-EnableScheduler` to enable existing scheduler |
| `Notifications:Telegram:Enabled` | Script pins false; automated fake HTTP tests validate delivery/retry without a real token |
| `VITE_API_BASE_URL` | Script pins empty same-origin mode; existing proxy targets localhost:5000 |

Root `.env.example` already documents DB/JWT/AI. [Minimal AI example](.env.example) contains just four empty/default AI variables. **Neither .NET nor these scripts automatically loads `.env` or `appsettings.Local.json`.** `.gitignore` excludes local `.env*` (except examples), `appsettings.Local.json`, `secrets.json`, private keys and build/runtime output directories. Merely copying a template does not configure the host.

This project has no `UserSecretsId`; do not assume `dotnet user-secrets` is automatically loaded or run `user-secrets init` as a required handoff step (it would edit the project). Use process environment variables for this unchanged architecture. Restart backend after changing AI settings; clear/re-login after JWT key changes. Do not expose the committed development JWT placeholder outside a local demo.

## 7. Demo login accounts

Existing seed development password: **`HoscoDemo!2026`**. This password already belongs to the repo's designed demo setup; passwords in the DB are PBKDF2 hashes. It is not a production credential.

| Role | Email | Effective scope |
|---|---|---|
| Owner | `owner@hosco.local` | HOSCO-A, assigned A-HCM only |
| Branch Manager | `branch.manager@hosco.local` | HOSCO-A, A-HCM only; rule PATCH forbidden |
| Chain Manager | `chain.manager@hosco.local` | HOSCO-A, A-HCM and A-HN |
| System Admin | `admin@hosco.local` | HOSCO-A, assigned A-HCM; not a cross-tenant superuser |
| Other tenant Owner | `owner@fixture.local` | HOSCO-B, assigned B-DN; isolation checks |

`GET /api/v1/reporting/branches` returns the authenticated user's permitted branch IDs. Do not invent IDs; use another demo account's response to exercise denied branch scope. Logout between roles; never attach JWTs/cookies to bug reports.

## 8. Mock / deterministic AI

```powershell
.\scripts\run-tester.ps1 -Service Backend -AiProvider Mock
```

Works without keys: UI chat, Chat API, deterministic intents, Greeting, Help, OutOfScope, Reporting/Alerts calls, RBAC and tenant/branch scope. Numbers come from existing Reporting API, not generated mock numbers. Provider outage/schema/retry errors are covered using fake HTTP in automated tests. No paid provider is contacted in this mode or the regression suite.

## 9. Live Gemini (optional, user-provisioned key)

Do not type a key literal into a command (shell history), share it in a screenshot or run a transcript capturing credentials. In the backend terminal only:

```powershell
$env:AI_PROVIDER = 'Gemini'
$env:AI_MODEL = Read-Host 'Gemini model ID supporting structured JSON (no URL or models/ prefix)'
$geminiInput = Read-Host 'GEMINI_API_KEY (hidden)' -AsSecureString
$geminiHandle = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($geminiInput)
try { $env:GEMINI_API_KEY = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($geminiHandle) }
finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($geminiHandle)
    $geminiInput.Dispose()
    Remove-Variable geminiInput,geminiHandle
}
try { .\scripts\run-tester.ps1 -Service Backend -AiProvider Gemini }
finally { Remove-Item Env:GEMINI_API_KEY -ErrorAction SilentlyContinue }
```

Select a model authorized for your account with the current adapter's `generateContent` structured JSON support; provisioning/model compatibility are tester/mentor choices, not a model availability promise. See [Gemini API reference](https://ai.google.dev/api/generate-content) and [local provider contract](../gd4/LLM_PROVIDER.md). Do not call it live PASS just because fallback returns HTTP 200.

## 10. Live OpenAI (optional, user-provisioned key)

```powershell
$env:AI_PROVIDER = 'OpenAI'
$env:AI_MODEL = Read-Host 'OpenAI model ID supporting Responses API strict structured JSON'
$openaiInput = Read-Host 'OPENAI_API_KEY (hidden)' -AsSecureString
$openaiHandle = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($openaiInput)
try { $env:OPENAI_API_KEY = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($openaiHandle) }
finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($openaiHandle)
    $openaiInput.Dispose()
    Remove-Variable openaiInput,openaiHandle
}
try { .\scripts\run-tester.ps1 -Service Backend -AiProvider OpenAI }
finally { Remove-Item Env:OPENAI_API_KEY -ErrorAction SilentlyContinue }
```

The existing adapter uses Responses API with strict structured intent schema, not free-form business calculations. Provider key belongs on the server, not in browser/client code, in accordance with [official OpenAI authentication guidance](https://developers.openai.com/api/reference/overview#authentication).

For either live provider, the run script fails **before startup** with `LIVE_LLM_PROVIDER=NOT_CONFIGURED` when model/key is missing. Direct application startup (without this script) intentionally falls back to deterministic AI for missing live key/model; invalid provider/settings fail validation. Network/HTTP/schema failures also fall back. No new status endpoint or logging was added. `-CheckOnly` validates presence/format, not provider acceptance or network connectivity. Do not print environment dumps.

## 11. Tests / full regression

From repo root, after setup (stop tester servers first to avoid build locks):

```powershell
dotnet build Hosco.slnx --no-restore
dotnet test tests/Hosco.UnitTests/Hosco.UnitTests.csproj --no-build
dotnet test tests/Hosco.IntegrationTests/Hosco.IntegrationTests.csproj --no-build
dotnet test Hosco.slnx --no-build
Push-Location src\Hosco.Web
try { npm.cmd run build } finally { Pop-Location }
git diff --check
git status --short
```

Integration tests start their own Kestrel API on a random high port with isolated SQLite and force Mock; they do not migrate your SQL Server or call a paid LLM. Current working-copy expected counts: Unit **141**, Integration **137** including the three uncommitted BUG-UI-001 regression cases and sixteen new Greeting/Help/security cases. Baseline HEAD alone has Unit 131, Integration 128. Confirm the delivered file/revision set before comparing counts.

## 12. Troubleshooting

| Symptom | Safe action |
|---|---|
| SDK not selected / NETSDK error | Check `dotnet --version` from repo root and `global.json`; install approved SDK 10.0.400 or compatible patch, not an arbitrary SDK band |
| `dotnet --info` Service Control Manager access denied only in sandbox | Compare same read-only command in a normal approved terminal; distinguish sandbox permission from project failure |
| `0xe0434352`, CI 3033/3077, HRESULT `0x800711C7` | Stop runtime attempts; collect matching Application events 1026/1000 and Code Integrity paths; mark `BLOCKED_BY_LOCAL_WDAC` if DLL was denied before app runs. Ask IT for approved signing/trusted environment; never bypass WDAC |
| npm.ps1 execution-policy error | Use `npm.cmd`; do not change security policy |
| `.ps1` execution blocked | Use manual commands or an approved signed script environment; do not run `Set-ExecutionPolicy`/Bypass |
| Port 5000/5173 occupied | Identify the owner; stop only your own server. StrictPort deliberately avoids silently switching API origin |
| Web API errors with plain `dotnet run` | Launch profile is 5179; use supplied script/explicit URL 5000 matching the proxy |
| DB connection / migration failure | Verify dedicated test connection, instance installed/running and account permissions; never show the full connection string/credentials in a ticket |
| 401 after restart | Ephemeral JWT key changed or token expired; logout/login |
| 403 | Check role + tenant + assigned branches; it is expected for denied scope, not something to bypass |
| Empty dashboard / zero revenue today | Seed sales are Jan–Jun 2026. Try that period; do not change the clock or fake current revenue |
| No new scheduled alerts | Scheduler default off; `-EnableScheduler` uses current time, not historical anomaly dates. Validate AL-01..05 historical fixtures via existing automated tests, not by changing business code |
| Telegram has no real delivery | Handoff script disables real Telegram. Fake-provider tests cover retry/backoff/idempotency. Do not label real delivery PASS |
| Live AI returns ordinary answer with no key | Direct API fallback is expected; script rejects missing config. HTTP 200 is not proof that a live LLM was used |
| `NU1900` vulnerability-feed warning | Record feed/network availability separately from compile/tests; don't run blanket package upgrades or security bypasses |
| Vietnamese `Ã`, `Ä`, `Æ`, `â€`, `�` artifacts | Compare API decoded name/description + Content-Type against DOM/build/DB. Valid Vietnamese `â` alone is not corruption. See [BUG-UI-001](../gd3/BUG-UI-001.md); remains Open pending affected tester evidence |

Read [checklist](TESTER_CHECKLIST.md) before reporting completion. Keep FAIL, NOT_CONFIGURED and ENVIRONMENT_BLOCKER distinct from PASS.
