# GD4 LLM Provider Status

CHATBOT DOES NOT ACCESS DATABASE DIRECTLY.

`ILlmProvider.TryResolveAsync` is an optional provider-independent intent abstraction. The registered implementation is **DisabledLlmProvider**, which returns no result. Supported Vietnamese phrases use the deterministic resolver first. Unknown phrases fall back to a safe unsupported response. Core runtime/tests do not require credentials or paid network calls.

No OpenAI/Gemini vendor has been selected or represented as BA-approved. No live LLM adapter, API key configuration, external phrasing generation or paid integration was implemented. Unit tests inject a fake provider to verify optional fallback. No new secret or `.env` field is required.

Any future provider must return typed candidates only, pass existing safety/authorization/allowlist, use bounded timeouts and safe telemetry, and never select URLs, emit executable SQL, query persistence or change reported numbers. Provider/vendor, privacy/data handling and production credential policy remain BA/mentor decisions.
