# GD4 Final Implementation Report

Date: 2026-10-06. Branch: `feature/gd4-ai-chatbot`, based on GD3 `d0b9f865a62a8f7a9dbcf5fb70d526fde2131b6a` (short checkpoint `d0b9f86`).

**GD4 PROPOSED MVP IMPLEMENTED AND LOCALLY VERIFIED.** BA approval is still pending; this is not a production deployment or a claim that the missing BA dataset was approved.

CHATBOT DOES NOT ACCESS DATABASE DIRECTLY.

## 1. Files changed

API: `Chat/ReportingApiClient.cs`, `Controllers/ChatController.cs`, `Observability/ExceptionHandlingMiddleware.cs`, `Program.cs`.

Application: `Abstractions/Chat.cs`, `Models/ChatModels.cs`, `Semantics/QueryCatalog.cs`, `Services/ChatIntentResolver.cs`, `ChatGuards.cs`, `ChatService.cs`, `ChatResponseComposer.cs`.

Web: `src/App.tsx`, `api/client.ts`, `components/Shell.tsx`, `pages/ChatPage.tsx`, `styles.css`, `types/api.ts`.

Tests: `Hosco.UnitTests/ChatbotTests.cs`, `Hosco.IntegrationTests/ChatApiTests.cs`, `ReportingApiClientTests.cs`, `Hosco.IntegrationTests.csproj`.

Documentation: all required GD4 plan/architecture/intents/mapping/security/testing/report/provider files, actual command logs, repeatable local runtime script, UI verification notes and raw browser screenshots under `docs/gd4/evidence`.

No domain entity, KPI formula, database migration or persistent chat-history schema was added. Package lock has no substantive diff from GD3.

## 2. Architecture implemented

Authenticated UI -> Chat API -> safety/resolver -> allowlist + scope guard -> real HTTP Reporting/Alerts API -> existing canonical reporting/persistence -> deterministic composer. No direct controller invocation, chatbot EF/SQL query or KPI formula duplication. Existing audit/correlation reused. HTTP Host cannot choose JWT destination; redirects/logging disabled, timeout 8s.

## 3. Supported intents

13 proposed intents: overview, revenue, GMV, total orders, AOV, gross profit, gross margin, cancellation/return rate, revenue trend, top/bottom products, dangerous inventory, current alerts. Seven date phrase categories use centralized UTC+7 helpers. Ambiguous period asks again; standalone period follow-up preserves metric, never scope. Details: [INTENTS](INTENTS.md).

## 4. Reporting API mapping

DashboardSummary, RevenueTrend, TopProducts, BottomProducts, DangerousInventory and AlertList plus scoped branch discovery. All are existing read endpoints, typed/fixed allowlist only. [Mapping](REPORTING_API_MAPPING.md) includes paths, catalog IDs, parameters and contract.

## 5. Security verification

Actual JWT 401, cross-branch/tenant 403, assigned BranchManager access, Owner/Admin assigned-scope restrictions, ChainManager tenant boundary, malicious Host/context and unsafe injection/SQL/secret requests verified. Invalid operation makes no HTTP call. Adapter tests verify 401/403/404/redirect/5xx, network, malformed data, timeout and cancellation. See [SECURITY](SECURITY.md) and runtime/full-suite logs.

## 6. Unit tests

Final **80/80 PASS**: 58 existing GD3 cases + 22 chatbot cases. Final authoritative execution is `dotnet test Hosco.slnx --no-build`; intermediate 79-test evidence is explicitly historical.

## 7. Integration tests

Final **80/80 PASS**: 46 GD3 cases + 34 GD4 live API/adapter cases. Kestrel fixture exercises the actual Chat -> authenticated HTTP Reporting path; faults use controlled handlers, no paid service.

## 8. Frontend build

Requested `npm.cmd install` PASS; TypeScript/Vite `npm.cmd run build` PASS. Initial sandbox Vite-temp EPERM cleared by running the same command in approved trusted environment, no source workaround. One transitive npm high advisory remains documented, not concealed.

## 9. Full regression

Solution build PASS (0 errors; NU1900 advisory lookup warning). Full solution tests PASS, total **160/160**. Existing Dashboard/export/reporting/alert/scope/SQLite concurrency and Telegram mock/persistence/retry tests still pass. No separate frontend test runner exists.

## 10. Runtime verification

Local SQLite seed API on port 5000; existing Vite proxy, UI on port 5176. Runtime script verified all 13 intents, ambiguity, unknown, multi-turn, no-data, 401, role/branch/tenant 403, injection refusal; GD3 XLSX/PDF exports HTTP 200 with correct content types (2822/2586 bytes).

Actual browser UI verified welcome, suggestions, messages, Top SKU, inventory, clarification, no-data follow-up, unknown, permission denied, clear, loading, API outage and retry recovery without duplicate messages. Skill computer-use guided browser observation; screenshots are real, unedited captures. See [UI evidence](evidence/UI_VERIFICATION.md).

Seed contains January–June data, so October-relative KPI/trend reads correctly return no data. Do not interpret that as failed API mapping or fabricate October amounts. Top/Bottom and current inventory returned structured seed values.

## 11. Known limitations / environment

- Deterministic phrase matching, not universal conversational intelligence. Confidence is heuristic.
- No live LLM adapter, comparative analytics, arbitrary historical calendar-date parsing, write/actions or persistent conversation history.
- Branch code/name must match exactly; optional scope is never retained between turns.
- Local Kestrel HTTP validated; HTTPS-only/reverse-proxy production setup not validated and fails closed without a suitable loopback listener.
- `source-map-js` high advisory from baseline npm dependencies remains open; no automatic package upgrade performed.
- NU1900 vulnerability-feed lookup warning; sandbox Vite EPERM and historical DPAPI keyring errors recorded. Final runtime is unblocked. No WDAC disabled, .NET reinstall or key deletion.

## 12. Git history

GD4 commits, chronological before this final documentation/evidence commit:

```text
c171e14 docs: add gd4 implementation plan
2ea2a50 feat: add chatbot domain and application contracts
8e873a1 feat: implement chatbot intent resolution
8b9d861 feat: add reporting api client and chat guard
2178def feat: add chatbot api and response composer
b1f8836 feat: add chatbot web ui
336b605 test: add gd4 chatbot unit coverage
7696d06 test: add gd4 chatbot integration coverage
7e8aa7a test: cover chatbot reporting failure fallback
f7169c6 fix: harden chatbot reporting boundary and intent context
a16c7b1 fix: handle chatbot retries and expired sessions
```

The final documentation commit follows these; its authoritative hash is reported from `git log`, not self-embedded in the file. Stage explicit paths, no all-repo add. No push/merge/history rewrite performed.

## 13. Git safety/status

Implementation commits are complete; only GD4 documentation/evidence remained before the final documentation commit. Final working-tree status is checked after that commit and reported to the user. Main remains `ed930682ac7b56f919fc0971535c19565bcd7a30`; GD3 checkpoint is untouched. No user/source files were deleted or reset, and no old tags altered.

## 14. Not implemented / BA pending

Missing final BA Intent Dataset/Conversation Design, copy/tone approval, provider/vendor and privacy/credential policy, comparisons, history/retention and production hosting. These are explicitly pending, not invented approved requirements. Real Telegram credentials and paid LLM calls are unnecessary for the proposed MVP and were not used.

See [TESTING](TESTING.md) and [Evidence index](evidence/README.md) for exact commands, results and limitations.
