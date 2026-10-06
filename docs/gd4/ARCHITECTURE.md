# GD4 Architecture

CHATBOT DOES NOT ACCESS DATABASE DIRECTLY.

## Runtime boundary

```text
Hosco.Web ChatPage -> POST /api/v1/chat/messages [ReportingReader/JWT]
  -> ChatService
     -> ChatSafetyGuard
     -> ChatIntentResolver [IBusinessTime + TimeProvider; disabled LLM fallback]
     -> ChatAuthorizationGuard [enum -> implemented QueryCatalog operation]
     -> ReportingApiClient [real HTTP; JWT + correlation ID]
        -> existing ReportingController / AlertsController
        -> existing ReportingScopeFactory / BranchScopeValidator
        -> canonical GD1/GD2/GD3 reporting/alert services -> persistence
     -> ChatResponseComposer [formats API values; no KPI recalculation]
     -> existing IAuditWriter [intent/operation/outcome only]
```

Application chat services depend on abstractions, not EF Core, DbContext, SQL or persistence repositories. The API controller reuses the existing correlation interface located in Infrastructure; it never queries persistence. Audit persistence is performed through the existing `IAuditWriter`, not a chatbot database query.

## Components

| Component | Responsibility |
|---|---|
| ChatModels / Chat abstractions | Typed request, intent, date, operation, response and interfaces |
| ChatIntentResolver | Vietnamese normalization, proposed phrase matching, inclusive business date ranges, bounded result limit, exact branch reference, simple period follow-up |
| ChatSafetyGuard | Required input / 1000-character cap; rejects unsafe requests |
| ChatAuthorizationGuard | Validates dates/limit/severity, implemented query allowlist, exact branch name/code from scoped API list |
| ReportingApiClient | Fixed paths, server-controlled loopback destination, forwarded JWT/correlation, no redirects, 8-second timeout, cancellation and safe failure mapping |
| ChatService | Orchestration, no-data/failure fallback, safe audit metadata |
| ChatResponseComposer | Deterministic Vietnamese answer from structured API values |
| ChatPage | Welcome, suggestions, message list, context, loading, error/403/retry, clear conversation |

## HTTP deployment assumptions

The client obtains the actual listener from `IServerAddressesFeature` and replaces its host with `127.0.0.1`. Incoming HTTP Host cannot control the bearer-token destination. Only known enum operations can construct paths. Automatic redirects and HttpClient logging are disabled.

Local Kestrel HTTP was verified. HTTPS-only deployments require a trusted loopback certificate/listener; reverse-proxy/IIS hosting with no advertised listener will return safe Unavailable. No TLS verification is disabled. Production hosting is not implemented or claimed validated.

## Data and state

Structured data is returned alongside the answer; the composer does not calculate revenue, GMV, AOV, margin, cancellation rate, ranking or available stock. Current alerts reuse authenticated `/api/v1/alerts`, not direct alert repository access. This is the existing scoped read API, documented in the chat query allowlist as `alerts.list.v1`.

Conversation context contains only previous intent/metric, no tenant or branch. Context is client-owned and untrusted. It is accepted only for a standalone period/follow-up phrase, then validated and authorized again. Unknown questions with a date are not treated as the previous metric. No persistent chat history or new database migration is introduced.

## Errors and observability

Unknown/Ambiguous/Unavailable are safe response statuses. Zero-order KPI responses and empty lists have explicit no-data messages. 401/403/404 remain HTTP failures; unexpected chat exceptions never disclose internal messages, even in Development. Client cancellation propagates. Audit records resolution, executed operation/outcome and unsafe-request refusals; existing correlation middleware supplies correlation ID. Prompts, bearer tokens and provider secrets are not placed in audit metadata.
