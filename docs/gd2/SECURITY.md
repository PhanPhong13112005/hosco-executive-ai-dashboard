# Security and Secrets Baseline

- JWT Bearer signature, issuer, audience, lifetime, and 30-second clock skew are validated.
- Demo credentials use PBKDF2-HMAC-SHA256 with 100,000 iterations; plaintext passwords are never stored.
- The committed signing key/connection string are explicit local-development placeholders. Production values must come from environment variables, a secret manager, or `dotnet user-secrets` for local work.
- `.gitignore` blocks `.env`, local appsettings, certificates, keys, and `secrets.json`.
- Reporting scopes always originate from authenticated claims, never from a client-supplied tenant.
- EF Core LINQ queries parameterize user filters. Sorting is selected from a code allow-list; no raw SQL or arbitrary SQL endpoint exists.
- Page size is capped at 200 and request/SQL command timeouts are 10 seconds.
- Correlation/request logs include identifiers and scope metadata, not passwords, raw JWTs, API keys, customer phone/email payloads, or stack traces outside Development.
- API errors use `{ code, message, correlationId }`. HTTPS termination and rate limiting should be enforced at the production ingress/API gateway.

## Pre-release checklist

Rotate the JWT signing key, use a least-privileged SQL login, disable Swagger unless explicitly required, add ingress rate limits, configure trusted proxies/CORS, and run dependency/secret/SAST scanning in CI.
