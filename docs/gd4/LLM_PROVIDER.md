# GD4 LLM API Integration — 2026-10-07

CHATBOT DOES NOT ACCESS DATABASE DIRECTLY.

`ILlmProvider.TryResolveAsync` is reused unchanged. OpenAI and Gemini now have `HttpClient` adapters; no vendor SDK/package was added. Default **Mock** mode uses `DisabledLlmProvider` plus the original deterministic resolver. Tests inject fake HTTP transports with structured provider responses; Mock mode does not invent reporting data.

## Configuration (server only)

| Variable | Default / allowed value |
|---|---|
| `AI_PROVIDER` | `Mock`; `OpenAI` or `Gemini` (case insensitive) |
| `AI_MODEL` | Empty; explicitly select an available model supporting this adapter's structured JSON API |
| `OPENAI_API_KEY` | Empty; used only for OpenAI |
| `GEMINI_API_KEY` | Empty; used only for Gemini |
| `AI_TIMEOUT_SECONDS` | 5; range 1–10, total budget including reads/backoff/retries |
| `AI_MAX_RETRIES` | 1; range 0–1 (at most two requests) |

`.env.example` is documentation only: .NET does **not** automatically read `.env`. Set actual process environment variables before starting `Hosco.Api`. A server secret manager can provision these names; the current project has no `UserSecretsId`, so automatic user-secrets loading must not be assumed. Restart the host after changing configuration. Never put keys in `src/Hosco.Web`, `VITE_*`, browser storage, CLI arguments, source control or screenshots. Avoid shell-history exposure when provisioning secrets.

Offline example (PowerShell):

```powershell
$env:AI_PROVIDER = 'Mock'
dotnet run --project src/Hosco.Api --no-build
```

For live use, set `AI_PROVIDER=OpenAI`/`Gemini`, `AI_MODEL=<supported-model-id>` and the corresponding key securely in the API process environment. No model/vendor is represented as BA-approved. Empty model or missing key selects deterministic fallback without an outbound call. Unknown provider, invalid model identifier, timeout or retry settings fail configuration validation with a safe message (not the supplied value). Endpoint URLs are constants, not configurable/user-selected.

## Provider contracts

- OpenAI: `POST https://api.openai.com/v1/responses`, Bearer key, `store:false`, strict `text.format` JSON schema, output capped at 2048 tokens. Only one completed assistant `output_text` is accepted; refusals/incomplete output fall back. Contract follows [OpenAI Structured Outputs](https://developers.openai.com/api/docs/guides/structured-outputs).
- Gemini: `POST https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent`, key in `x-goog-api-key` (never the URL), `responseMimeType:application/json`, `responseJsonSchema`, 2048 output tokens. Only a single `STOP` candidate/text part is accepted; blocked/truncated/function/thought/multiple-part output falls back. Fields are documented in [Gemini GenerateContent / GenerationConfig](https://ai.google.dev/api/generate-content).

Adapters have bounded retries for 429/5xx/network failure; other HTTP failures, redirects and malformed/schema-invalid output return no candidate. Provider timeout uses one cancellation budget across all attempts; caller cancellation propagates. HTTP logging and automatic redirects are disabled, response read capped at 64 KiB and intent JSON at 8,192 characters. Raw provider errors, prompts and keys are never included in chat responses/audit. No live model compatibility, billing, latency or provider availability is claimed from mock tests.

## Structured intent and fallback

Required fields, no extras/duplicates:

```json
{"intent":"Revenue","status":"Resolved","from":"2026-05-01","to":"2026-05-31","branch":null,"limit":null,"metric":"revenue","severity":null,"comparison":"None","confidence":0.95}
```

Local `LlmIntentContract` validates exact enum names (13 supported intents + Unknown), status, numeric confidence 0–1, canonical metric, paired inclusive calendar dates, ordered range ≤366 days (years 1900–9998), branch name/code (not GUID/URL/instruction), limits 1–20 only for list intents, and allow-listed severity only for alerts. Dates become UTC instants through existing `IBusinessTime` (UTC+7), not an LLM-supplied timezone. List intents default to limit 5. Required KPI/trend dates missing, Ambiguous or confidence <0.75 cause a server-authored clarification without reporting execution. Unsupported intent/schema => deterministic fallback; explicit valid Unknown => safe unsupported response.

`comparison=None|PreviousPeriod` is extracted/validated. PreviousPeriod asks which period to view separately; no new comparison endpoint, KPI arithmetic or invented deltas. Follow-up includes only a validated previous intent and server-derived canonical metric, never branch/tenant scope; invalid client context is dropped. Provider prompt receives current business date, current user question and this minimal context, **not** JWT, keys, branch directory, KPI values or DB data.

Flow: safety → configured LLM → locally validated candidate (or deterministic fallback) → mandatory authorization/allowlist → real Reporting/Alerts HTTP API → unchanged deterministic composer. LLM has no DB/SQL tools, arbitrary URL execution or authority to alter access. Guard re-resolves requested branch against the authenticated user's scoped directory; Reporting API independently enforces JWT/RBAC/tenant/branch. Reporting API remains source of truth; composer never recalculates values.

The keyword safety guard is defense-in-depth, not a claim of universal prompt-injection detection. Structured validation plus authorization and fixed API paths enforce the actual security boundary. User question text can contain sensitive information; production provider/privacy/retention approval and credential provisioning remain mentor/BA decisions.

## Validation status

`LIVE_LLM_PROVIDER=NOT_CONFIGURED`

No key/model/provider variables were configured in Process/User/Machine environments during validation. No paid API was called. Build PASS; Unit 131/131, Integration 128/128, full regression 259/259, frontend build PASS. HTTP provider contract/failure tests and real Kestrel Chat → Reporting tests used **mock LLM HTTP**, not a live external model.

When keys become available, live smoke must verify an actual validated provider candidate (not merely HTTP 200 from deterministic fallback), with: revenue today; top 5 this week; dangerous inventory; "Còn tuần trước?" after Revenue; and one unsafe prompt (403 before provider). Record PASS/FAIL and provider/model only, never key/prompt/error body; missing keys stay NOT_CONFIGURED. Automated tests must remain offline.

See [testing/evidence](TESTING.md#llm-extension-validation--2026-10-07) for actual commands and current limitations.
