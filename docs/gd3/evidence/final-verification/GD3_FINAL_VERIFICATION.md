# GD3 Final Functional Verification

## Executive result

**C. GD3 HAS FUNCTIONAL FAILURES**

Verification ran against commit `9c7dae510da8e8f102747253fb253425dcbe7df0` on branch `feature/gd3-dashboard-alert` on 2026-09-29 (Asia/Saigon). Source code was not changed.

Build, all 58 unit tests, frontend build, API startup, the main UI flows, exports, scheduler execution, alert workflow, Telegram mock channel, and security checks produced real runtime or automated-test evidence. The integration suite ran 44 tests and reported 43 passed and 1 failed. The failed notification retry-persistence test is a real SQLite compatibility defect, not WDAC. A second SQLite runtime problem was reproduced when the Dashboard loaded several endpoints concurrently.

## Environment

- OS: Windows NT 10.0.26200, win-x64
- PowerShell: 7.6.5
- .NET SDK: 10.0.400
- .NET runtime: 10.0.11
- Node.js: 26.7.0
- npm: 11.19.0
- Frontend: Vite development runtime and production build
- API runtime providers tested: SQLite in-memory and EF InMemory
- Expected commit: `9c7dae510da8e8f102747253fb253425dcbe7df0`
- Git was clean before evidence creation.

See [environment.txt](logs/environment.txt).

## Verification matrix

| # | Feature | Method | Result | Evidence |
|---:|---|---|---|---|
| 1 | Build | `dotnet build Hosco.slnx --no-restore` | PASS | [build.txt](logs/build.txt): 0 errors; NU1900 environment warning only |
| 2 | Unit tests | xUnit console run | PASS | [unit-tests.txt](logs/unit-tests.txt): 58/58 passed |
| 3 | Integration tests | xUnit console run | FAIL | [integration-tests.txt](logs/integration-tests.txt): 43/44 passed; SQLite `DateTimeOffset` ORDER BY failure |
| 4 | Database migration | Fresh LocalDB `HoscoGd3Evidence_20260929` attempt; pending-model check | NOT_RUN | [database-validation.txt](logs/database-validation.txt): LocalDB automatic instance unavailable; pending model changes = none |
| 5 | API startup | Real API process, `/health/ready`, Swagger | PASS | [api-runtime.txt](logs/api-runtime.txt); health HTTP 200; database Healthy |
| 6 | Authentication | Real login and unauthenticated request | PASS | [api-functional.txt](logs/api-functional.txt): login 200; no token 401 |
| 7 | Tenant/Branch scope | Runtime random branch plus automated scope cases | PASS | [api-functional.txt](logs/api-functional.txt): 403; integration branch/cross-tenant tests passed |
| 8 | Dashboard | Real UI against SQLite and InMemory | FAIL | InMemory UI passed, but SQLite UI repeatedly returned 500 `database is locked`; [api-runtime-sqlite.txt](logs/api-runtime-sqlite.txt) |
| 9 | Filters | UI date filter and branch-scoped API | PASS | [ui-observations.txt](logs/ui-observations.txt); [api-functional.txt](logs/api-functional.txt) |
| 10 | 8 KPI | UI display plus API cross-check | PASS | KPI table below; [dashboard-summary.json](logs/dashboard-summary.json) |
| 11 | Trend | Real UI chart and API request | PASS | [ui-observations.txt](logs/ui-observations.txt); API runtime HTTP 200 |
| 12 | Top/Bottom | Real UI tables and API response | PASS | [product-ranking.json](logs/product-ranking.json) |
| 13 | Dangerous Stock | UI table and API response | PASS | [api-functional.txt](logs/api-functional.txt): two SKU rows |
| 14 | Drilldown | Opened KPI-01 drill-down in UI | PASS | [ui-observations.txt](logs/ui-observations.txt) |
| 15 | Empty state | 2028 date range in UI | PASS | Zero/N/A KPIs; trend/rankings showed no data; [ui-observations.txt](logs/ui-observations.txt) |
| 16 | Error state | API stopped intentionally, UI reloaded | PASS | UI showed connection message and retry; [ui-observations.txt](logs/ui-observations.txt) |
| 17 | Excel export | UI trigger, authenticated HTTP download, ZIP/OpenXML inspection | PASS | [dashboard-2026-hcm.xlsx](exports/dashboard-2026-hcm.xlsx); [export-validation.txt](logs/export-validation.txt) |
| 18 | PDF export | UI trigger, authenticated HTTP download, `%PDF` and pypdf parse | PASS | [dashboard-2026-hcm.pdf](exports/dashboard-2026-hcm.pdf); one readable page; [export-validation.txt](logs/export-validation.txt) |
| 19 | Scheduler | Real startup cycle with valid interval | PASS | [scheduler-runtime-valid.txt](logs/scheduler-runtime-valid.txt): 20 evaluated, 4 created, 0 failed |
| 20 | AL-01 | Executed unit boundaries and integration signal query | PASS | [unit-tests.txt](logs/unit-tests.txt); [integration-tests.txt](logs/integration-tests.txt) |
| 21 | AL-02 | Executed unit boundaries and integration peak-window query | PASS | Same test logs |
| 22 | AL-03 | Executed tests plus runtime scheduler alert creation | PASS | Same test logs; [scheduler-alert-delivery.txt](logs/scheduler-alert-delivery.txt) |
| 23 | AL-04 | Executed evaluator tests and runtime role/workflow | PASS | [security-workflow.txt](logs/security-workflow.txt) |
| 24 | AL-05 | Executed discount/floor-price boundary and signal tests | PASS | Unit/integration logs |
| 25 | Alert persistence | Integration persistence/dedup test | PASS | `Dangerous_stock_rule_persists_alert_and_deduplicates_second_cycle` passed |
| 26 | Alert Center | Real UI after scheduler | PASS | Six open alerts observed; [ui-observations.txt](logs/ui-observations.txt) |
| 27 | Alert Detail | Real UI/API detail | PASS | Actual/threshold/status/delivery observed; [scheduler-alert-delivery.txt](logs/scheduler-alert-delivery.txt) |
| 28 | Rule configuration | Real UI list plus protected API integration | PASS | AL-01 through AL-05 rendered; BranchManager save denied |
| 29 | Cooldown | Executed unit and persistence tests | PASS | Unit cooldown test and integration second-cycle test passed |
| 30 | Dedup | Executed unit/integration tests | PASS | Repeated dispatch and dangerous-stock dedup tests passed |
| 31 | Escalation | Executed unit test and runtime escalation delivery | PASS | [unit-tests.txt](logs/unit-tests.txt); [scheduler-alert-delivery.txt](logs/scheduler-alert-delivery.txt) |
| 32 | Acknowledge | Runtime AL-04 request as BranchManager | PASS | [security-workflow.txt](logs/security-workflow.txt): 200, status Acknowledged |
| 33 | Resolve | Runtime AL-04 request as BranchManager | PASS | [security-workflow.txt](logs/security-workflow.txt): 200, status Resolved |
| 34 | Resolution note | Runtime missing-note and valid-note cases | PASS | Missing note 400; valid note 200 |
| 35 | Audit | Executed actor/time tests and runtime actor/time response | PASS | Unit audit test; runtime `acknowledgedBy`, `resolvedBy`, timestamps |
| 36 | Telegram channel | Fake HTTP integration tests | PASS | Success, disabled provider, timeout, 5xx and safe recipient tests passed |
| 37 | Retry/backoff | Unit behavior passed; SQLite persistence retry path failed | FAIL | [integration-tests.txt](logs/integration-tests.txt) |
| 38 | Idempotency | Executed unit and integration tests | PASS | Repeated dispatch sent once and persisted once |
| 39 | Delivery persistence | Initial/idempotent persistence passed; retry persistence failed | FAIL | Runtime delivery rows exist, but failed retry query blocks complete verification |
| 40 | Permission/403 | Runtime and automated security checks | PASS | No token 401; cross scope 403; BranchManager config 403; Owner AL-04 403 |

## KPI cross-check

Scope: HOSCO Ho Chi Minh branch `fdb80dd6-cf56-9599-86f7-1e2be3386090`, 2026-01-01 through 2026-06-30, UTC+7.

| Metric | UI | API | Match |
|---|---:|---:|---|
| Revenue | 77,710,000 VND | 77,710,000 | YES |
| GMV | 78,230,000 VND | 78,230,000 | YES |
| Orders | 510 | 510 | YES |
| AOV | 152,373 VND | 152,372.5 | YES, UI rounded |
| Gross Profit | 33,804,000 VND | 33,804,000 | YES |
| Gross Margin | 43.5% | 43.50% | YES |
| Cancellation/Return Rate | 8.25% | 8.25% | YES |
| Dangerous Stock | 2 | 2 | YES |

## Alert rule execution

| Rule | Executed | Alert generated/evaluated | Evidence |
|---|---|---|---|
| AL-01 | YES | Unit boundaries, integration baseline query, seeded runtime alert and escalation delivery | Unit/integration logs; scheduler delivery log |
| AL-02 | YES | Unit boundaries, integration peak-window query, seeded runtime alert and escalation delivery | Unit/integration logs; scheduler delivery log |
| AL-03 | YES | Integration persistence/dedup plus runtime scheduler generated alerts | Scheduler: two visible branch-scoped alerts |
| AL-04 | YES | Unit/integration evaluation and runtime acknowledge/resolve/permission flow | Security workflow log |
| AL-05 | YES | Unit discount/floor boundaries and integration per-order-item query | Unit/integration logs |

Scheduler runtime summary: 20 rule/branch evaluations, 4 alerts created, 0 duplicates suppressed during that first cycle, and 0 rule failures.

## Exports

### Excel

- HTTP 200, MIME `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- Size: 2,838 bytes
- ZIP/OpenXML opened successfully with 166 cells.
- Contains metadata, selected date range, branch, eight KPI rows, open/urgent alerts, Top SKU, Bottom SKU, and dangerous inventory.

### PDF

- HTTP 200, MIME `application/pdf`
- Size: 2,586 bytes
- Header `%PDF-1.4`
- pypdf opened one page and extracted the selected scope, KPI summary, Top/Bottom SKU, and dangerous inventory.
- The in-app PDF viewer returned a black canvas, so a reliable visual PNG could not be persisted. Parser validation confirms the file is readable and not corrupt.

## Notification verification

- Telegram fake HTTP integration tests executed successfully for enabled success, disabled provider, timeout, server error and safe recipient key behavior.
- Unit tests executed retry-after-backoff, max-attempt failure, non-retryable terminal behavior and idempotent repeated dispatch.
- Scheduler runtime persisted safe recipient keys without destinations or secrets.
- Runtime delivery states included `Skipped`, attempt count 1, timestamp, purpose `Initial` or `Escalation`, and `Telegram channel is disabled.`
- The SQLite retry-persistence integration path failed before it could complete because `DateTimeOffset` cannot be used in SQLite `ORDER BY` translation.

## Security and workflow results

- Valid Owner login: HTTP 200.
- Dashboard without token: HTTP 401.
- Random/cross-scope branch: HTTP 403.
- Automated same-tenant branch and cross-tenant checks passed.
- BranchManager rule update: HTTP 403 and UI permission message.
- Owner AL-04 acknowledge: HTTP 403.
- BranchManager AL-04 acknowledge: HTTP 200.
- AL-04 resolve without note: HTTP 400.
- AL-04 resolve with a note: HTTP 200 and final status `Resolved`.

## Failures found

### F-01: SQLite notification retry query fails

- Reproduction: run the 44-test integration suite.
- Failing test: `AlertEnginePersistenceTests.Notification_failure_and_retry_count_are_persisted`.
- Error: `System.NotSupportedException: SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses.`
- Technical location: `src/Hosco.Infrastructure/Persistence/NotificationDeliveryStore.cs`, line 38, called by `NotificationDispatcher.RetryPendingAsync`.
- Impact: retry/backoff and retry-count persistence cannot be considered fully verified on the SQLite provider.
- This is a code/provider compatibility failure, not WDAC.

### F-02: SQLite dashboard runtime can fail under concurrent UI load

- Reproduction: start the API with `Database:Provider=Sqlite`, log in from the frontend, and let Dashboard request summary/trend/rankings/inventory concurrently.
- Observed: Dashboard error state; `/api/v1/reporting/dashboard/summary` returned HTTP 500.
- Error: `SQLite Error 5: database is locked`.
- Stack location: `AlertRepository.GetSummaryAsync` called by `ReportingController.Dashboard` while using the shared SQLite in-memory runtime connection.
- Sequential authenticated API calls returned HTTP 200. The same UI flow passed with the supported EF InMemory provider.
- This is a reproducible local-provider concurrency failure, not WDAC.

## Environment blockers and warnings

- Fresh SQL Server LocalDB migration could not run: LocalDB could not create an automatic instance. No production database was touched.
- `dotnet ef migrations has-pending-model-changes --no-build` reported no model changes after the latest migration.
- Build warning NU1900: NuGet vulnerability feed was unavailable. Build still completed with 0 errors.
- API startup emitted local Data Protection key access/decryption warnings, but health, authentication and application endpoints remained operational.
- No new WDAC evidence occurred during this run: 0 matching Application 1000/1026 events and 0 matching Code Integrity 3033/3077 events. See [wdac.txt](logs/wdac.txt).

## Screenshot Index

The browser tool produced genuine live screenshots inline during verification but did not expose a supported filesystem path for saving those bytes. Therefore no `.png` file was fabricated in `screenshots/`. Each requested capture is recorded below as `SCREENSHOT_UNAVAILABLE`, with raw runtime evidence supplied instead.

| Intended filename | Function evidenced | Timestamp | Reproduction | Observed result |
|---|---|---|---|---|
| `01-build-pass.png` | Build | 2026-09-29 19:44 +07 | Run build command | SCREENSHOT_UNAVAILABLE; build log PASS |
| `02-unit-tests.png` | Unit tests | 2026-09-29 19:45 +07 | Run unit suite | SCREENSHOT_UNAVAILABLE; 58/58 log PASS |
| `03-integration-tests-FAIL.png` | Integration tests | 2026-09-29 19:45 +07 | Run integration suite | SCREENSHOT_UNAVAILABLE; 43/44 and stack trace logged |
| `04-api-startup.png` | Swagger/API startup | 2026-09-29 20:08 +07 | Open Swagger UI | Live screenshot captured inline; repository PNG unavailable |
| `05-login.png` | Owner login | 2026-09-29 19:49 +07 | Submit demo account | Live browser result observed; token not displayed |
| `06-dashboard.png` | Dashboard and KPI | 2026-09-29 19:54 +07 | InMemory API and default filter | Live screenshot captured inline |
| `07-dashboard-empty.png` | Empty state | 2026-09-29 19:56 +07 | Select 2028 range | Live screenshot captured inline |
| `08-dashboard-error.png` | Controlled API outage | 2026-09-29 19:57 +07 | Stop API and reload | Live screenshot captured inline |
| `09-top-bottom.png` | SKU rankings | 2026-09-29 19:55 +07 | Scroll Dashboard | Live screenshot captured inline |
| `10-dangerous-stock.png` | Dangerous inventory | 2026-09-29 19:55 +07 | Scroll Dashboard | Live screenshot captured inline |
| `11-drilldown.png` | KPI-01 drill-down | 2026-09-29 19:55 +07 | Open Revenue card | Live screenshot captured inline |
| `12-export-xlsx.png` | Excel export | 2026-09-29 19:59 +07 | Trigger UI and inspect workbook | SCREENSHOT_UNAVAILABLE; workbook saved and structurally validated |
| `13-export-pdf.png` | PDF export | 2026-09-29 19:59 +07 | Trigger UI and open PDF | Viewer black canvas; parser validation used |
| `14-alert-center.png` | Alert Center | 2026-09-29 19:52 +07 | Open Alert navigation | Live screenshot captured inline |
| `15-alert-detail.png` | Alert detail | 2026-09-29 19:53 +07 | Open AL-03 fixture | Live screenshot captured inline |
| `16-alert-config.png` | AL-01 through AL-05 configuration | 2026-09-29 19:53 +07 | Open config page | Live screenshot captured inline |
| `17-permission-403.png` | BranchManager denied rule update | 2026-09-29 20:06 +07 | Save AL-01 as BranchManager | Live screenshot captured inline |
| `18-notification-delivery.png` | Telegram delivery state | 2026-09-29 20:05 +07 | Open scheduler-created AL-03 | Live screenshot captured inline |
| `19-wdac-event.png` | WDAC | 2026-09-29 20:07 +07 | Query event logs | NOT_APPLICABLE; no matching new events |

## VERIFIED PASS

- Build and frontend production build
- Unit tests 58/58
- API health, Swagger, login and unauthenticated rejection
- InMemory Dashboard, filters, eight KPI, trend, Top/Bottom, dangerous stock, drill-down, empty and error states
- Excel and PDF generation/content
- Scheduler valid startup cycle
- AL-01 through AL-05 automated evaluator/signal coverage
- Alert Center, alert detail and rule configuration display
- Acknowledge, resolve, resolution note enforcement and actor/time capture
- Telegram mock channel, disabled provider, timeout and server-error behavior
- Cooldown, dedup, escalation and idempotency tests
- Runtime delivery records and safe recipient keys
- Tenant/branch/RBAC checks

## BLOCKED

- Fresh SQL Server migration/schema inspection: LocalDB automatic instance unavailable.
- Repository PNG persistence: live screenshots were visible in the browser tool, but the tool did not provide a supported filesystem-save path.

## FAILED

- SQLite notification retry-persistence query (`DateTimeOffset` ORDER BY translation).
- SQLite dashboard concurrent UI load (`database is locked`).

## BA/PENDING

- Previous-period comparison definition
- Branch performance definition
- Cashier performance definition

## Evidence files

- `logs/build.txt`
- `logs/unit-tests.txt`
- `logs/integration-tests.txt`
- `logs/database-validation.txt`
- `logs/api-runtime.txt`
- `logs/api-functional.txt`
- `logs/security-workflow.txt`
- `logs/scheduler-runtime-valid.txt`
- `logs/scheduler-alert-delivery.txt`
- `logs/export-validation.txt`
- `logs/frontend-build.txt`
- `logs/wdac.txt`
- `logs/ui-observations.txt`
- `exports/dashboard-2026-hcm.xlsx`
- `exports/dashboard-2026-hcm.pdf`

## Git safety

- No business logic or source file was edited.
- No commit, push, merge, rebase, reset, restore, cherry-pick or branch change was performed.
- Only evidence files under `docs/gd3/evidence/final-verification/` were created.

## Remediation Verification

This section records the follow-up remediation. The original failure evidence above is intentionally retained.

### Failure 1: notification retry persistence

- Original failure: `Notification_failure_and_retry_count_are_persisted` failed because EF Core SQLite rejected `ORDER BY` over `DateTimeOffset` in `NotificationDeliveryStore.GetRetryableAsync`.
- Root cause: the query ordered tracked retry candidates by nullable `LastAttemptAt` and `CreatedAt` directly in SQLite SQL. SQLite's EF provider does not support translating those `DateTimeOffset` order expressions.
- Fix: status filtering remains database-side. SQL Server keeps server-side ordering and limiting. SQLite materializes only the `Pending` candidate set and applies the same `LastAttemptAt`, `CreatedAt` ordering in memory, with `Id` as a deterministic final tie-break. Terminal `Failed`, `Sent`, and `Skipped` deliveries remain excluded; schema, idempotency, retry policy, and tenant data are unchanged.
- Targeted test: the original persistence test plus a new SQLite regression test for status filtering and deterministic ordering passed 2/2. See [notification-retry-remediation.txt](logs/notification-retry-remediation.txt).
- Final result: full integration suite passed 46/46, including retry persistence; notification retry is PASS.

### Failure 2: concurrent SQLite dashboard load

- Original failure: concurrent dashboard/supporting endpoint load produced SQLite Error 5 (`database is locked`), HTTP 500 responses, and degraded readiness.
- Root cause: the SQLite runtime registered one open `Data Source=:memory:` physical connection as a singleton and supplied that same connection to every scoped `HoscoDbContext`. Parallel requests concurrently initialized and used the same `SqliteConnection`; the captured stack failed in `SqliteRelationalConnection.InitializeDbConnection` / `SqliteConnection.CreateFunctionCore`, before business queries executed. The reproduction produced 63 HTTP 500 and 7 HTTP 503 responses from 90 requests. This was connection concurrency, not a long transaction, scheduler, seed, audit, or dashboard business-rule defect.
- Fix: the SQLite-only runtime path now uses a uniquely named shared in-memory database, retains one DI-managed keep-alive connection, and gives every scoped `DbContext` its own connection from the connection string. A 30-second SQLite busy timeout safely accommodates legitimate single-writer contention. The SQL Server production path, scheduler, audit, and security behavior are unchanged.
- Targeted test: a new integration regression issued 72 dashboard, reporting, health, and alerts requests concurrently and passed 1/1. See [sqlite-concurrency-remediation.txt](logs/sqlite-concurrency-remediation.txt).
- Final result: the post-fix external runtime replay returned HTTP 200 for all 90 concurrent requests, followed by health 200, dashboard 200, and alerts 200. No `database is locked`, HTTP 500/503, or unhandled exception appeared in the post-fix API logs. See [sqlite-concurrency-runtime-after-fix-client.txt](logs/sqlite-concurrency-runtime-after-fix-client.txt) and [sqlite-concurrency-runtime-after-fix-api.txt](logs/sqlite-concurrency-runtime-after-fix-api.txt).

### Final validation after remediation

- Build: PASS, 0 errors; NU1900 warning only because the NuGet vulnerability feed was unavailable. See [build-after-fix.txt](logs/build-after-fix.txt).
- Unit tests: PASS, 58/58. See [unit-tests-after-fix.txt](logs/unit-tests-after-fix.txt).
- Integration tests: PASS, 46/46. See [integration-tests-after-fix.txt](logs/integration-tests-after-fix.txt).
- Frontend was not changed and was not rebuilt, per the remediation scope.
- No schema change or migration was introduced.
- The transient `dotnet.exe` dialog observed during the manual replay came from launching the API with the repository root as its content root, so `appsettings.json` was not loaded and the application threw the explicit `Jwt:SigningKey` validation exception. Starting from the API output directory loaded the existing configuration and completed runtime verification; this dialog was not WDAC or Code Integrity evidence.
