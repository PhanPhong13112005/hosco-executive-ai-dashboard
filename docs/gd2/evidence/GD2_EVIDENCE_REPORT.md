# GD2 Evidence Report

## Repository

`D:\\Code\\hosco-executive-ai-dashboard`, branch `chore/gd2-evidence`.

## Commit Audited

`f75ac38c323ac2b61dd127081cc742852200f197`; tag `gd2-complete` resolves to the same commit. Source diff from that commit was empty before evidence generation.

## Date

2026-09-15 (Asia/Saigon).

## Environment

Windows 10.0.26200, .NET SDK 10.0.400, runtime 10.0.11, EF CLI 10.0.11, SQL Server LocalDB 17.0.4025.3.

## GD2-01 Repository & Project Structure

Status: VERIFIED

Evidence: `Hosco.slnx` contains Domain, Application, Infrastructure, API, UnitTests, and IntegrationTests. Project references match the intended dependency direction. All six restored and built.

## GD2-02 ERD / Data Dictionary / Seed Dataset

Status: PARTIALLY VERIFIED

Evidence: 16 application tables were counted from model/migration; SQL was generated and the migration applied to LocalDB. SQL queries confirmed two tenants, four branches, 2,084 orders across 2026-01-01..2026-06-30, related records, and four anomaly types. Some BranchId columns lack physical branch FKs.

## GD2-03 Auth / Tenant / Branch Scope

Status: VERIFIED

Evidence: JWT claims/validation, claim-derived tenant scope, branch validation, PBKDF2 hashes, tests, and SQL-backed 401/403/200 behavior were verified.

## GD2-04 Semantic Layer / Query Catalog

Status: PARTIALLY VERIFIED

Evidence: eight KPI contracts and 11 versioned allow-listed query IDs exist; catalog/filter tests pass. Only the order list is implemented status, the KPI summary is provisional, and BA formulas remain blocked.

## GD2-05 Observability / Audit

Status: PARTIALLY VERIFIED

Evidence: correlation generation/reuse, structured request metadata, exception mapping, production-safe error message behavior, audit schema/writer, and order-list usage exist. Full endpoint/business audit coverage does not.

## GD2-06 Secrets / Env / Health

Status: PARTIALLY VERIFIED

Evidence: required ignore patterns and configuration boundaries exist; live and database-ready checks returned 200 on LocalDB. No production credential was identified, but tracked demo credential material remains a review finding and cold-start liveness is database-dependent.

## GD2-07 Reporting API v1

Status: VERIFIED

Evidence: five reporting routes plus login were found in source/OpenAPI. SQL-backed runtime verified login, authorized reporting, 401, 403, correlation reuse, health, and Swagger.

## Automated Validation

Build: PASS — 0 warnings, 0 errors.

Tests: 28/28 PASS — 11 unit, 17 integration, 0 failed, 0 skipped.

Migration artifacts: `20260914161316_InitialCreate` verified; no pending model changes; idempotent SQL generated.

DB Apply: PASS — migration applied and seeded on SQL Server LocalDB.

Dependency Audit: PASS query; no vulnerable packages from current feeds; outdated candidates documented and not updated.

Secret Audit: PARTIAL — no identified production credential; development/demo material and scanner limitations documented.

## BA Pending Items

- Revenue recognition: included statuses, discount treatment, refund timing/allocation.
- GMV: included/excluded order states and gross basis.
- Total orders: status inclusion and time semantics.
- AOV: numerator, denominator, exclusions, and zero-order behavior.
- Gross profit/margin: `UnitCostAtSale` use, refund/return allocation, margin denominator.
- Cancellation/return rate: count versus value and denominator.
- Product ranking: revenue versus quantity, tie handling, and time/status rules.
- Dangerous stock: threshold source, comparison rule, and branch/product overrides.

## Known Limitations

- Integration tests use SQLite; SQL Server behavior is covered by separate migration/runtime checks, not by the automated suite.
- Seed is deterministic for an empty database but does not repair partial data.
- AuditWriter is invoked only for order-list reporting.
- Health failure behavior was inspected in code; the shared LocalDB service was not intentionally disrupted.
- OpenAPI does not list direct-mapped health endpoints.
- Dependency and secret scans are point-in-time/basic checks, not a replacement for CI scanning.

## Conclusion

The GD2 technical foundation is executable: all projects build, 28 tests pass, the migration applies, deterministic data exists on SQL Server, and the secured API/OpenAPI run. Evidence does not claim BA approval or complete audit coverage. The documented integrity, cold-start health, semantic-labeling, demo-credential, and query-warning findings require owner triage; no business code was altered.

