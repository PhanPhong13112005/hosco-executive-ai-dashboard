# HOSCO tester checklist

Use [setup](TESTER_SETUP.md), [AI smoke tests](LIVE_AI_SMOKE_TEST.md) and [handoff report](HANDOFF_REPORT.md). Fill each result as PASS / FAIL / NOT_CONFIGURED / ENVIRONMENT_BLOCKER / NOT_RUN; an unchecked box is not proof of a defect or completion.
Record delivered revision + local changes, date/time, database mode, provider/model and role. Do not record secrets/JWTs.

- [ ] **Login** — all five seed accounts; bad password rejected; logout/token-expiry; 401 when unauthenticated.
- [ ] **Dashboard** — date/branch filters, loading/empty/error states; use Jan–Jun 2026 for seeded sales.
- [ ] **KPI** — 8 KPI cards, trends and drill-down match Reporting API's canonical semantics; no frontend/chat recomputation.
- [ ] **Reporting** — allow-listed queries, Top/Bottom, dangerous stock, pagination; correct role/date/branch.
- [ ] **Smart Alert** — alert creation, severity, cooldown/dedup/escalation and persistence; in-memory SQLite restart is deliberately reset, not persistence evidence.
- [ ] **AL-01 → AL-05** — evaluate anomaly fixtures with existing automated tests; document expected thresholds as configurable proposals, not immutable business truth.
- [ ] **Alert Center** — list/filter/detail, acknowledge/resolve, audit; allowed and denied roles.
- [ ] **Alert Configuration** — rule labels/description, enabled/severity/threshold/window/cooldown/JSON, validation and permissions. Use disposable DB if editing.
- [ ] **Notification** — fake Telegram transport, status/retry/backoff/idempotency + delivery persistence tests; no key required. Real Telegram remains NOT_CONFIGURED unless explicitly provisioned separately.
- [ ] **Excel export** — download xlsx; open with compatible spreadsheet viewer; correct headers/data/scope and no corrupted Vietnamese.
- [ ] **PDF export** — download PDF; open/read pages, correct Vietnamese/data/scope; HTTP/content-type alone is not visual PDF proof.
- [ ] **AI Chatbot** — send/render/history/follow-up; correlation IDs; business data matches Reporting API.
- [ ] **Greeting** — `Hello`, `Xin chào!`; local greeting, no reporting/LLM; `intent/status=Unknown` is the unchanged non-reporting contract.
- [ ] **Help** — `Bạn làm được gì?`, `Help`; local capabilities within authorized scope, no reporting/LLM.
- [ ] **OutOfScope** — weather question returns unsupported, no invented data or reporting call.
- [ ] **Live LLM** — optional Gemini/OpenAI. Without actual key/model mark `LIVE_LLM_PROVIDER=NOT_CONFIGURED`; HTTP 200 from fallback is not live PASS.
- [ ] **RBAC** — BranchManager rule PATCH 403; Owner AL-04 action denied; SystemAdmin is not implicitly all-branch/all-tenant.
- [ ] **Tenant isolation** — HOSCO-A cannot read HOSCO-B; changing supplied scope or injecting `tenant` cannot grant access.
- [ ] **Branch isolation** — BranchManager/Owner/Admin A-HCM only; ChainManager A-HN allowed, B-DN denied.
- [ ] **Vietnamese encoding** — rule names/descriptions/time ranges, chat, dashboard/export, API UTF-8 and browser DOM; compare content, not just the font family.
- [ ] **Error handling** — 400/401/403/404, unavailable Reporting, safe fallback; no secrets/prompts/raw provider errors in UI/logs.
- [ ] **Retry** — mocked provider 429/5xx/network, time budget; notification retry/backoff/idempotency and concurrent locking tests.
- [ ] **Regression** — setup build, Unit, Integration, full solution regression, frontend build, `git diff --check`; record actual counts and zero skips.
- [ ] **Persistent database** — separate SQL Server migration + restart persistence if in scope; SQLite in-memory does not satisfy this gate.
- [ ] **Secrets** — no keys in frontend/VITE/browser storage, repo/test/docs/logs/screenshots; no environment dumps or unredacted HAR.

## BUG-UI-001 focused retest

Open "Cấu hình cảnh báo" as Owner/ChainManager and inspect all five rules. Expected canonical examples:

- AL-01: **Tỷ lệ hủy đơn bất thường**; "So sánh tỷ lệ hủy 1 ngày với baseline 7 ngày cùng chi nhánh."
- AL-02: **Doanh thu giờ cao điểm giảm**; "So sánh khung 11:00–13:00 và 17:00–20:00 UTC+7 với 7 ngày trước."
- AL-03/04/05: inspect complete names/descriptions, not only the first two visible cards.

Look for suspicious mojibake sequences involving `Ã`, `Ä`, `Æ`, `â€` or replacement `�`. **A standalone `â` is a valid Vietnamese letter**, not sufficient evidence of corruption. Check API decoded strings + `application/json; charset=utf-8`, `document.characterSet`, then DOM/text and screenshot. Do not apply blanket string replacement or reseed a shared DB.

If reproduced, report frontend URL, API/build/revision, DB identifier (no credentials), role/rule code, timestamp, sanitized name/description response + Content-Type, and screenshot. Redact tokens/cookies. [BUG-UI-001](../gd3/BUG-UI-001.md) remains Open awaiting the originally affected tester environment; current clean seed regression PASS is not closure evidence for that environment.
