# Reporting API v1 Verification

The API was started from the audited build against SQL Server LocalDB with seed and Swagger explicitly enabled. The process was stopped after verification.

## Runtime checks

| Method | Route | Expected | Actual | Status |
|---|---|---|---|---|
| GET | `/health/live` | Process healthy | 200; `Healthy`, no dependency checks | VERIFIED |
| GET | `/health/ready` | Database reachable | 200; database check `Healthy` | VERIFIED |
| POST | `/api/v1/auth/login` | Valid configured demo identity returns JWT | 200; token retained in memory and not recorded | VERIFIED |
| GET | `/api/v1/reporting/orders?pageSize=1` | Valid owner JWT returns tenant-scoped data | 200; one row, total count 1,042 | VERIFIED |
| GET | `/api/v1/reporting/orders?pageSize=1` | No JWT is rejected | 401 | VERIFIED |
| GET | `/api/v1/reporting/orders?branchId=<out-of-scope>&pageSize=1` | Branch manager outside assignment is rejected | 403 | VERIFIED |
| GET | `/swagger/v1/swagger.json` | OpenAPI available when enabled | 200; OpenAPI 3.0.4 | VERIFIED |

A supplied `X-Correlation-ID` value was returned unchanged on the authenticated request.

## Source routes

| Method | Route | Handler |
|---|---|---|
| POST | `/api/v1/auth/login` | Login |
| GET | `/api/v1/reporting/kpis/summary` | Technical KPI preview |
| GET | `/api/v1/reporting/revenue` | Revenue trend preview |
| GET | `/api/v1/reporting/orders` | Paged order list |
| GET | `/api/v1/reporting/products/ranking` | Product ranking preview |
| GET | `/api/v1/reporting/inventory/dangerous` | Dangerous-inventory preview |

## OpenAPI

The captured text artifact contains six paths (the controller routes above), a Bearer JWT security scheme, and no detected credential value. Health endpoints are mapped directly and therefore are not present in the controller-generated OpenAPI document.

## Health semantics

`/health/live` deliberately runs no registered dependency check. `/health/ready` runs the tagged database check and returns unhealthy when `CanConnectAsync` fails. During this audit both were healthy. A database-unavailable run was not induced against the shared LocalDB instance. Cold startup currently applies migrations before listening, which can prevent the live endpoint from becoming available if SQL Server is already unavailable; this is recorded as a defect.

Verdict: `VERIFIED` for the tested SQL-backed runtime paths.

