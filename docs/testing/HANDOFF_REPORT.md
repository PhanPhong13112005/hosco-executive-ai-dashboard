# Tester handoff — validation report

This report preserves the **local preparation audit before commit/push**. Git references and the untracked-file snapshot below describe that historical checkpoint, not the branch's current published HEAD. Subsequent publication is authorized separately; verify its commit hashes and remote status with `git log`, `git status` and `git branch -vv`. No live-provider or tester bug-closure claim is implied by publication.

Date: 2026-10-07. Repository: `D:\Code\hosco-executive-ai-dashboard`.
Branch unchanged: `feature/gd4-ai-chatbot`.
HEAD unchanged: `835829895f1329b1ca53bf6ee9d4102c8ac6af63`.
Main reference unchanged: `ed930682ac7b56f919fc0971535c19565bcd7a30`.
No commit, push, merge, branch switch, reset, restore, rebase or cherry-pick performed for this handoff.

## Delivery and changes

Prepared [setup](TESTER_SETUP.md), [checklist](TESTER_CHECKLIST.md), [AI smoke cases](LIVE_AI_SMOKE_TEST.md), minimal four-variable [.env example](.env.example), and three PowerShell scripts (`setup-tester.ps1`, `run-tester.ps1`, `tester-common.ps1`). README links the new handoff entry point.

Initial smoke found **Hello/Help returned unsupported** in the existing deterministic mode, despite that being a required tester scenario. Minimal fix adds exact normalized greeting/capability phrases in existing `ChatIntentResolver` and calls that local route **after the safety guard and before the LLM** in `LlmIntentResolver`. No architecture, endpoint, enum, provider schema, DB/KPI/reporting logic or permission changes. Local replies use existing `Unknown` non-reporting contract. Added 10 Unit and 6 Integration cases for variants, real Mock chat response, zero fake-provider/reporting calls and mixed greeting/injection denial.

Preserved the six pre-existing untracked BUG-UI-001 files (report, three text evidence files, screenshot, and AlertRuleEncodingTests: five docs/evidence files + one test file). No font workaround, blanket Unicode replacement or DB reseed was performed. BUG-UI-001 remains Open awaiting originally affected tester evidence.

At the preparation checkpoint, scripts/documents/fix/tests were **uncommitted/local**. They are grouped for subsequent reviewed commits/publication on `feature/gd4-ai-chatbot`; the cloning instructions refer to the complete published revision, not baseline `8358298`. This historical report alone is not proof of a successful push.

## Audit

| Item | Observation |
|---|---|
| .NET | SDK 10.0.400; ASP.NET/.NET runtime 10.0.11; matches global.json |
| Node/npm | Node v26.7.0, npm 11.19.0; Vite 8.3.0 accepts installed version |
| Database configuration | Default appsettings SQL Server / LocalDB HoscoDev; quick-start explicitly chooses isolated SQLite in-memory |
| Migration/seed | Existing SQL Server `MigrateAsync`; SQLite `EnsureCreatedAsync`; seed initializes two tenants/four branches/five users + Jan–Jun 2026 fixtures. Existing-tenant seed performs business fixture updates, not a no-op |
| JWT | Existing committed development placeholder; run script instead generates a per-process random signing key if none provided, without display/write |
| AI | AI_PROVIDER/model/keys absent in Process/User/Machine; default Mock. Existing missing-model/key fallback documented; run script gives explicit pre-start NOT_CONFIGURED error for live selection |
| Ports | Existing launch profile 5179 conflicts with proxy default5000; run script pins backend5000/no launch profile and frontend5173/strictPort |
| Accounts | Existing demo password mechanism documented; Owner/BranchManager/Admin assigned A-HCM, ChainManager HOSCO-A all branches, fixture Owner B-DN |

## Actual validation results

Commands executed in an approved trusted PowerShell 7 context, with Mock/fake providers and isolated SQLite. No Windows security change or paid LLM call.

| Gate | Result |
|---|---|
| `scripts/setup-tester.ps1` | PASS: .NET restore, explicit EF tool manifest restore (10.0.11), solution build, npm ci and frontend build |
| Final `dotnet build Hosco.slnx --no-restore` | PASS, 0 errors; 1 existing NU1900 vulnerability-feed/network warning |
| `dotnet test tests/Hosco.UnitTests/Hosco.UnitTests.csproj --no-build` | **141/141 PASS**, 0 failed, 0 skipped |
| `dotnet test tests/Hosco.IntegrationTests/Hosco.IntegrationTests.csproj --no-build` | **137/137 PASS**, 0 failed, 0 skipped |
| `dotnet test Hosco.slnx --no-build` | **278/278 PASS** (141 Unit +137 Integration), 0 failed, 0 skipped |
| Final `npm.cmd run build` | PASS, TypeScript check + Vite production build |
| Script parsing/CheckOnly (PowerShell7) | PASS, all three scripts; backend/frontend checks, missing live model/key rejected for both vendors without displaying values |
| Windows PowerShell5.1 `.ps1` invocation | **ENVIRONMENT_BLOCKER**: running scripts disabled by current execution policy. No Bypass/Set-ExecutionPolicy used; not marked script runtime PASS. Manual alternative documented |
| Backend run script | PASS, localhost5000, Development, SQLite seeded, Mock; readiness200 |
| Frontend run script | PASS,127.0.0.1:5173; frontend HTTP200, readiness through existing proxy200, unauthenticated proxy API401 |
| Copy/paste AI smoke block from LIVE_AI_SMOKE_TEST | **9/9 PASS in Mock** after Greeting/Help fix; TC01..06 HTTP200 with expected operation/null, TC07..09 HTTP403 forbidden |
| Alert rule encoding | HTTP200,5 Owner rules, application/json;charset=utf-8; AL01/02 canonical Vietnamese correct; full encoding integration cases pass |
| Export HTTP runtime | xlsx200 correct MIME/2824bytes; pdf200 application/pdf/2586bytes with historical seed filter. Download transport verified; not a new visual document inspection |
| SQL Server migration/restart-persistence gate | **NOT_RUN** for this handoff (avoid modifying shared LocalDB). Dedicated DB procedure documented; SQLite pass is not SQL Server/persistence evidence |
| Real Telegram | **NOT_CONFIGURED**; run script pins disabled. Existing fake HTTP delivery/retry/idempotency tests pass in regression |
| Live OpenAI/Gemini | **LIVE_LLM_PROVIDER=NOT_CONFIGURED**; no real key/model and no paid requests. Fake provider tests are not live verification |
| Git whitespace | `git diff --check` PASS; Git may print LF-to-CRLF advisory messages, not whitespace errors |

The initial sandbox-only `dotnet --info` Service Control Manager access denial did **not** reproduce outside sandbox: the identical read-only command returned complete SDK/runtime info with exit0. Runtime/tests here ran successfully in trusted context; this is not proof Enterprise WDAC is disabled or removed. Historical local Code Integrity3033/3077, .NET Runtime1026/Application Error1000, HRESULT0x800711C7 remain documented in [known environment issues](../gd3/KNOWN_ENVIRONMENT_ISSUES.md). If a tester is denied before application startup, classify as `BLOCKED_BY_LOCAL_WDAC`, retain exact command/event/module evidence and ask IT for an approved environment; do not call it business logic failure or bypass it.

## Secret audit (redacted, working-copy scope)

Only FOUND / NOT FOUND classifications; credential values are not written to this report.

| Category | Result |
|---|---|
| Real-format OpenAI key (`sk-...`) in current tracked/non-ignored source, tests, docs, config and text evidence | **NOT FOUND** |
| Real-format Gemini key (`AIza...`) | **NOT FOUND** |
| Real-format Telegram bot token or hardcoded serialized JWT in current text inventory | **NOT FOUND** |
| Real provider key in Process/User/Machine environment | **NOT FOUND** |
| Provider credentials in frontend source/build artifacts | **NOT FOUND** |
| Existing development JWT placeholder / demo password / synthetic fake-provider credentials | **FOUND — development/test fixtures only**, not production/provider credentials |
| Local secret configuration candidates besides examples in repo | **NOT FOUND** |
| Ignoring `.env`, `.env.local`, web `.env.local`, appsettings.Local.json and secrets.json | **FOUND — ignore rules active** |

Audit includes key/token patterns and key assignments, with manual classification of the existing development JWT placeholder and synthetic 13-character Telegram fixture token used with StubHandler. No production key was requested or obtained. This is a current-text inventory audit, not a forensic assertion covering deleted Git history, encrypted/binary archives, arbitrary unknown secret formats or external machines. Future live provisioning must be environment-only and must not appear in VITE/browser/logs/fixtures/docs. The frontend run subprocess removes inherited backend credential variables; app provider HttpClient logging remains disabled.

## Remaining tester gates

Tester should perform interactive UI checks, export visual QA, persistent SQL Server tests if required, and optional live provider validation with their own authorized credentials. Record NOT_CONFIGURED/UNVERIFIED distinctly from PASS; see the checklist. Do not close BUG-UI-001 until retest/evidence from the affected environment supports closure.

## Pre-publication Git / runtime snapshot

Both temporary tester services started for verification were stopped using Ctrl+C in their own sessions (Vite's batch confirmation was answered). The stop exit code is not a validation failure. No unrelated process was stopped.

New commit hashes: **NONE**. HEAD and main remain the references shown above. No files were staged or removed. Handoff changes comprise 6 tracked modifications +8 new files; the 6 pre-existing BUG-UI-001 files remain separate and intact (20 changed/untracked paths total).

Preparation checkpoint `git status --short --untracked-files=all` (historical, not post-push status):

```text
 M README.md
 M src/Hosco.Application/Services/ChatIntentResolver.cs
 M src/Hosco.Application/Services/LlmIntentResolver.cs
 M tests/Hosco.IntegrationTests/ChatApiTests.cs
 M tests/Hosco.IntegrationTests/LlmChatApiTests.cs
 M tests/Hosco.UnitTests/LlmIntentTests.cs
?? docs/gd3/BUG-UI-001.md
?? docs/gd3/evidence/bug-ui-001-alert-rules.jpg
?? docs/gd3/evidence/bug-ui-001-api.txt
?? docs/gd3/evidence/bug-ui-001-build.txt
?? docs/gd3/evidence/bug-ui-001-regression.txt
?? docs/testing/.env.example
?? docs/testing/HANDOFF_REPORT.md
?? docs/testing/LIVE_AI_SMOKE_TEST.md
?? docs/testing/TESTER_CHECKLIST.md
?? docs/testing/TESTER_SETUP.md
?? scripts/run-tester.ps1
?? scripts/setup-tester.ps1
?? scripts/tester-common.ps1
?? tests/Hosco.IntegrationTests/AlertRuleEncodingTests.cs
```
