# GD4 Validation

CHATBOT DOES NOT ACCESS DATABASE DIRECTLY.

## LLM extension validation — 2026-10-07

Current implementation extends checkpoint `eab5850`; the October 6 evidence below is preserved as **pre-LLM baseline**, not live provider validation.

| Command / gate | Final result | Evidence |
|---|---|---|
| `dotnet build Hosco.slnx --no-restore` | PASS, 0 errors, 1 NU1900 advisory-feed warning | `evidence/logs/llm-final-build.txt` |
| `dotnet test Hosco.slnx --no-build` | Unit **131/131**, Integration **128/128**, total **259/259**, no failures/skips | `evidence/logs/llm-full-regression.txt` |
| `npm.cmd run build` from `src/Hosco.Web` | PASS TypeScript + Vite; no frontend source/dependency change | `evidence/logs/llm-frontend-build.txt` |
| External live OpenAI/Gemini | **NOT_CONFIGURED**, no paid calls | `evidence/logs/llm-validation-status.txt` |

Individual suites also passed 131 Unit / 126 Integration before two final DI cases were added. Final solution run above is authoritative. An intermediate follow-up assertion incorrectly expected a date label in a no-data answer: final test observes actual HTTP Reporting `from/to` instead, without changing composer/business logic. Failed intermediate output is retained in `llm-intermediate-assertion.txt` and is **not** a final failure.

Added **51 unit cases**: configuration/default/missing credentials/unsafe identifiers/bounds, exact schema/types/duplicates/extra fields, intent/metric/date/branch/limit/severity validation, UTC+7, low-confidence/ambiguity/comparison clarification, canonical context, unsafe prompts before provider, deterministic fallback, caller cancellation.

Added **48 integration cases**: real OpenAI/Gemini adapter wire contracts with fake HTTP handlers; DI selects configured adapters/scoped resolver; completed/refusal/incomplete/malformed/oversized JSON, network/429/5xx/retry recovery, no retry for 401/403/redirect, timeout vs caller cancellation, missing key => no network. In-process Kestrel + isolated seed SQLite executes Mock LLM HTTP → parser → Chat API/JWT → guard → actual authenticated Reporting HTTP → composer/audit. Revenue data equals direct Reporting API values; branch/date/follow-up extraction, assigned scope/unauthorized/cross-tenant 403, 401 before provider, six requested injection/SQL/key/prompt refusals, malformed/unavailable fallback, low confidence with zero Reporting calls are verified.

All baseline KPI, dashboard/export, alerts, notification/Telegram mock, retry/concurrency and original chat tests remain included. Process API fixture explicitly forces `AI_PROVIDER=Mock` and removes inherited provider keys; the LLM fixture uses only fake headers/handler, never an external network call. No API key provisioning, WDAC change, .NET reinstall, SQL/business rewrite or dependency upgrade occurred.

Initial restricted build exited 1 with no compiler diagnostic; the same solution compiled/tested successfully in the approved outside-sandbox environment. That initial output alone does not establish a new WDAC event. No WDAC bypass or source workaround was used. Frontend build passed in the normal workspace. Existing NuGet/npm/environment notes below remain historical/known limitations; no npm audit/upgrade or new browser UI smoke was performed in this extension.

`LIVE_LLM_PROVIDER=NOT_CONFIGURED`: boolean-only checks found OpenAI/Gemini keys, AI_PROVIDER and AI_MODEL absent in Process/User/Machine environments. Live five-question smoke is pending credentials, not represented as PASS by mock tests. [Configuration, live checklist and privacy boundary](LLM_PROVIDER.md).

## Pre-LLM baseline — 2026-10-06

## Final executed results

| Command | Result | Evidence |
|---|---|---|
| `dotnet build Hosco.slnx --no-restore` | PASS, 0 errors; NU1900 advisory-feed warning | `evidence/logs/final-build.txt` |
| `dotnet test Hosco.slnx --no-build --logger 'console;verbosity=normal'` | PASS: Unit 80/80, Integration 80/80, no skipped tests | `evidence/logs/full-regression.txt` |
| `npm.cmd install` | PASS, 24 packages audited; 1 high advisory | `evidence/logs/npm-install.txt` |
| `npm.cmd run build` | PASS TypeScript + Vite | `evidence/logs/final-frontend-build.txt` |
| `Verify-ChatRuntime.ps1` against local API | PASS actual HTTP/runtime responses incl. 401/403 and XLSX/PDF | `evidence/logs/chat-api-runtime.txt` |
| Browser Chat UI | PASS observed welcome, Top SKU, inventory, ambiguity, follow-up, unknown, 403, loading, unavailable/retry and clear | `evidence/UI_VERIFICATION.md`, real JPG screenshots |

The full solution command is authoritative for final counts. Earlier `final-unit-tests.txt` captured 79 tests before the last parameter-validation test; it is retained as intermediate evidence, not the final result. Earlier build/test logs are likewise historical phase evidence.

## Coverage

Final suite includes 58 GD3 unit + 22 chatbot unit cases, and 46 GD3 integration + 34 GD4 integration/adapter cases. Chat API tests launch a real Kestrel process and call the authenticated chat -> reporting HTTP flow. Adapter fault tests use fake HttpMessageHandler/IServer; they are not represented as live network outages of a production reporting service.

Covered: Vietnamese intents/dates, exact UTC+7 boundary, ambiguity, safe unknown/context, every suggestion, optional fake provider, allowlist/scope/parameter checks, composer numeric/no-data, unavailable service fallback; authentication, all role scopes/tenant isolation, unsafe prompts, validation, forged Host/context; HTTP status faults, timeout/cancellation, network and invalid envelope.

Full regression includes existing KPI/seed/ranking, Dashboard/API/export, alerts/rules/workflow, cooldown/dedup/escalation, SQLite retry/concurrent locking, notification persistence/idempotency/backoff and fake Telegram channel validation. No paid LLM or real Telegram credentials used. Frontend has no test script; frontend build and actual browser smoke checks are the executed gates.

## Runtime fixture and reproduction

API binary is run from `src/Hosco.Api/bin/Debug/net10.0` with `--Database:Provider=Sqlite --Seed:Enabled=true --Swagger:Enabled=true --Logging:LogLevel:Default=Warning --urls=http://127.0.0.1:5000`. Vite uses port 5176 and the existing proxy to localhost:5000. Only local demo accounts and seed data are used.

Seed contains January–June 2026 data. Calendar-relative KPI/trend questions on October 6 correctly show no data; runtime does not claim fabricated October revenue. Top/Bottom no-date ranking and current dangerous inventory return real seed data. Composer numeric formatting is verified with structured unit fixtures. Export smoke returns XLSX 2822 bytes / PDF 2586 bytes, HTTP 200; export formatting/permission regression is also covered by existing integration tests.

Run from repo root (PowerShell):

```powershell
dotnet build Hosco.slnx --no-restore
dotnet test Hosco.slnx --no-build
& ./docs/gd4/evidence/Verify-ChatRuntime.ps1
```

## Environment/dependency notes

- WDAC did not block final build/tests/API. No Code Integrity policy was disabled or .NET reinstalled. Earlier GD3 WDAC history is retained separately.
- Vite build initially hit sandbox EPERM creating `.vite-temp` config; the exact unchanged build passed in the approved trusted environment.
- Sandbox API startup logged DPAPI keyring decrypt/access errors (historical `api-runtime.txt`); JWT/demo chat nevertheless worked. Final API ran outside sandbox. No DataProtection keys were deleted or security settings changed.
- NuGet advisory lookup warning NU1900 is not a build/code failure; package restore/build succeeded.
- Read-only `npm audit` reports GHSA-68fv-2mgg-jv7q (`source-map-js <1.2.2`, high DoS); `evidence/logs/npm-audit.txt` preserves exact output. No `npm audit fix` was run or dependency upgraded. This known dependency issue remains open for a separately reviewed update.
