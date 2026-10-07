# BUG-UI-001 — Alert Configuration Vietnamese encoding

Validation date: 2026-10-07. Branch: `feature/gd4-ai-chatbot`.
Application checkpoint: `835829895f1329b1ca53bf6ee9d4102c8ac6af63`.
Severity: Medium. Reporter status: **Open**.
Investigation outcome: **NOT_REPRODUCED_ON_CURRENT_BUILD / AWAITING_TESTER_RETEST**.
This is not a claim that the affected tester environment has been fixed or that the bug is closed.
Build/test and Git statements below record the encoding investigation checkpoint, before later Greeting/Help changes and commit/publication. For the later 141 Unit /137 Integration tester baseline see [handoff validation](../testing/HANDOFF_REPORT.md). Publishing this evidence does not close the bug.

## Reported behavior

After login → Cấu hình cảnh báo, AL-01/AL-02 titles/descriptions and time ranges contain `Ã`, `Ä`, `Æ`, `â...` instead of Vietnamese. Expected: all rule text retains correct accents and is readable. The supplied screenshot is consistent with mojibake (wrongly decoded text); it does not by itself identify whether corruption originated in stored data, an older binary, an API response or client processing.

## Executed checks

- Working tree was clean at investigation start. Source `DemoSeed.ApplyRuleDefaults` contains correct Vietnamese AL-01 → AL-05 names/descriptions, including AL-02 `11:00–13:00` / `17:00–20:00`.
- `src/Hosco.Web/index.html` declares `lang="vi"` and UTF-8. `RulesPage` renders `rule.name`/`rule.description` directly; the API client uses `response.json()` with no manual recoding. No mojibake markers were found in relevant application source.
- Read-only SQL queries against existing `(localdb)\MSSQLLocalDB` / `HoscoDev`: 10 rules; `Name`/`Description` are `nvarchar`; zero rows match the reported markers using binary collation. Names/descriptions were readable. These are older rule descriptions, not the same dataset as the current isolated seed fixture. LocalDB history contains only InitialCreate and AddDashboardAlertFoundation; no migration/reseed/UPDATE was performed.
- Existing development servers were initially stopped. Verification used the current API binary with **isolated in-memory SQLite**, seed enabled, scheduler disabled and `AI_PROVIDER=Mock`; frontend was the existing Vite source on port 5176. This did not modify HoscoDev or invoke a paid provider.
- Authenticated `GET /api/v1/alert-rules` returned 200, `application/json; charset=utf-8`, and correct decoded text for all five rules. See [API evidence](evidence/bug-ui-001-api.txt).
- Using computer-use/browser inspection, logged in as the local demo Owner and opened Cấu hình cảnh báo. Browser DOM had all five correct titles/descriptions, `document.characterSet=UTF-8`, and computed heading font `"Segoe UI", system-ui, sans-serif`. No rule config was saved or toggled. The actual current viewport screenshot shows AL-01 correctly; all five DOM strings were inspected separately. Full-page screenshot capture failed, so the successful unedited viewport capture is retained rather than claiming a full-page visual check. See [screenshot](evidence/bug-ui-001-alert-rules.jpg).

## Regression coverage / results

Added `tests/Hosco.IntegrationTests/AlertRuleEncodingTests.cs` (3 cases: Owner, ChainManager, other-tenant Owner). Uses the existing real HTTP/Kestrel seed fixture, strict UTF-8 decoding, JSON parsing, exact expected name/description equality for AL-01 → AL-05 and response charset assertion. It checks decoded JSON strings, not raw `\uXXXX` escapes (which are valid JSON). No application source, CSS/font, seed data, KPI/alert logic or migration changed.

- `dotnet build Hosco.slnx --no-restore`: PASS, 0 errors; existing NU1900 advisory-feed warning.
- `dotnet test Hosco.slnx --no-build`: Unit **131/131**, Integration **131/131**, total **262/262 PASS**, zero failures/skips. Includes the 3 new encoding cases and all prior LLM/reporting/alert/security tests.
- [Build output](evidence/bug-ui-001-build.txt), [regression output](evidence/bug-ui-001-regression.txt).

No frontend source changed, so npm dependencies/build were not rerun for this regression-only addition. No commit/push/merge/branch change/history rewrite, WDAC change, .NET reinstall, database reseed or speculative string-replacement/font workaround.

## Root cause status / next evidence needed

**Exact root cause is not yet established.** Current source → isolated seed database → API → rendered DOM preserves Vietnamese. Existing LocalDB also did not contain the reported corruption. An older process/build, a different tester database, stale in-memory/browser state or incorrect decoding in that specific environment remain hypotheses, not findings.

Tester should retest the exact affected environment and capture:

1. Frontend URL/port, API destination, application commit/build and database/provider identifier (no credentials).
2. DevTools Network → `GET /api/v1/alert-rules`: Content-Type and the AL-01/AL-02 `code`, `name`, `description` from the response; redact Authorization, cookies and other secrets. Do not send an unredacted HAR.
3. The same strings as rendered in the page. If API data is already corrupt, inspect stored values and the import/seed/build path. If API data is correct but DOM text is corrupt, investigate that frontend build/transformation/cache. If both API and DOM Unicode are correct but glyphs render incorrectly, then investigate the font/glyph path.

No blanket reseed or mojibake replacement is authorized: those could reset rule settings, audit/configuration or valid text. A repair must target the proven source of corruption and preserve thresholds/configuration, scope and business logic. Keep BUG-UI-001 Open until the reporter's affected environment is verified.
