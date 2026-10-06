# GD4 Security

CHATBOT DOES NOT ACCESS DATABASE DIRECTLY.

## Trust boundaries

- Chat requires JWT + existing `ReportingReader` policy.
- All business reads cross the real authenticated HTTP API. No controller shortcuts, EF/SQL calls, write endpoints or shell tools are exposed to chat.
- Intent is mapped to implemented catalog entries and fixed operation paths. Unknown enum values never send HTTP. Invalid provider dates/limits/severity are rejected.
- Branch reference matches only scoped `/reporting/branches` output. Missing/inaccessible branch returns generic 403 without revealing its existence.
- Owner and SystemAdmin require assigned branch scope; ChainManager stays within its tenant; BranchManager only assigned branches.
- Every turn reuses current JWT authorization. Conversation context carries neither branch nor tenant. Extra client context fields cannot override scope.
- Listener-derived loopback origin prevents malicious Host from receiving JWT. Redirects are disabled; no arbitrary URL/HTTP proxy. TLS validation remains enabled.

## Safety and secrets

Safety blocks explicit bypass/prompt instructions, tenant/branchId requests, SQL, connection strings, API key/system prompt, commands and URLs. Input length is 1–1000. This conservative phrase filter is defense-in-depth, not a claim of universal prompt-injection detection. Hard allowlist/read-only/authenticated scope are the decisive protections. No external LLM/tool executor is active.

Audit includes intent/operation/outcome and existing correlation, not raw prompt, JWT, key or connection string. HttpClient logging is removed. Unexpected chat errors hide internal messages even in Development. 401/403/404 and timeout/network/5xx have safe handling; caller cancellation is not converted into an artificial success.

## Executed verification

| Case | Actual result | Evidence |
|---|---|---|
| No bearer token | 401 | ChatApiTests + runtime log |
| BranchManager A-HCM / A-HN | 200 / 403 | ChatApiTests + runtime log |
| Owner/Admin A-HN | 403 | ChatApiTests + runtime log |
| ChainManager A-HN / B-DN | 200 / 403 | ChatApiTests + runtime log |
| Other-tenant owner A-HCM | 403 | ChatApiTests |
| Unsafe tenant/branchId/SQL/secret/prompt requests | 403 | Unit, API integration, runtime log |
| Malicious HTTP Host | Still uses loopback and succeeds within scope | Adapter + live ChatApiTests |
| Forged context scope | No branch/tenant context accepted or returned | ChatApiTests |
| Unknown operation | No outgoing HTTP | ReportingApiClientTests |
| 401/403/404/302/500/503 | Safe typed errors, no response body leaked | ReportingApiClientTests |
| Timeout / cancellation / network / malformed JSON | Safe unavailable or cancellation propagation | ReportingApiClientTests |

Tests passed, but this is not a penetration-test certification. Baseline npm audit reports one high advisory in transitive `source-map-js`; see TESTING and evidence. No automatic dependency upgrade, WDAC weakening or .NET reinstall was performed.
