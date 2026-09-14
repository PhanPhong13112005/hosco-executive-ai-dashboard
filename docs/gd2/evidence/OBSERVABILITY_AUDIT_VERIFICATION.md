# Observability and Audit Verification

## Correlation and request logging

`CorrelationMiddleware` accepts a non-empty value up to 128 characters containing only letters, digits, dash, underscore, or dot. Missing/invalid values are replaced with a 32-character GUID and every response receives `X-Correlation-ID`.

The structured request log includes path, status, elapsed milliseconds, correlation ID, user ID, tenant ID, branch claims, and query ID. It does not log request bodies, authorization headers, raw JWTs, passwords, or API keys. `Valid_correlation_id_is_reused` passed, and manual runtime verification also reused the supplied ID.

## Exception mapping

| Exception | HTTP/code |
|---|---|
| `ValidationException` | 400 / `validation_error` |
| `ForbiddenException` | 403 / `forbidden` |
| `KeyNotFoundException` | 404 / `not_found` |
| `BusinessDefinitionPendingException` | 409 / `business_definition_pending` |
| Request-aborted `OperationCanceledException` | 499 / `request_cancelled` |
| Other | 500 / `internal_error` |

Responses include code, message, and correlation ID. For unhandled errors, the exception message is returned only in Development; production returns a generic message. The logger records exception details server-side.

## Audit baseline

`AuditLog` and `IAuditWriter`/`AuditWriter` are real infrastructure. The writer stores tenant, current user, optional branch/resource/query, correlation ID, serialized metadata, and timestamp. The orders endpoint invokes it for `orders.list.v1`; the SQL-backed verification created one row.

Infrastructure exists: `VERIFIED`.  
Endpoint usage on order listing: `VERIFIED`.  
Full business/reporting audit coverage: `NOT VERIFIED` (the other reporting endpoints do not call the writer).

Overall verdict: `PARTIALLY VERIFIED`.

