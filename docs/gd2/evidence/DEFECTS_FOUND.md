# Defects Found

No business code was changed. These findings are left for owner triage.

## GD2-DEF-01 — Incomplete branch referential integrity

- Severity: Medium
- Files: `src/Hosco.Infrastructure/Persistence/HoscoDbContext.cs:44-46`; initial migration
- Problem: `Refund.BranchId`, `Alert.BranchId`, and `AuditLog.BranchId` have no physical foreign key to `Branches`. Separate tenant and branch foreign keys elsewhere also do not enforce that the selected branch belongs to the selected tenant.
- Evidence: SQL Server `sys.foreign_keys` contains branch FKs only for Employees, Inventories, Orders, and UserBranches.
- Suggested fix: decide whether nullable historical branch references are intentional; otherwise add relationships and tenant-consistent composite constraints in a new migration.

## GD2-DEF-02 — Cold-start liveness depends on database migration

- Severity: Medium
- File: `src/Hosco.Api/Program.cs:25-29,145-151`
- Problem: `InitializeDatabaseAsync` runs `MigrateAsync` before `RunAsync`. If SQL Server is unavailable at cold start, the process does not listen and `/health/live` cannot report process health, despite the live check itself being dependency-free.
- Evidence: direct code path inspection; healthy SQL-backed runtime was verified, but the shared LocalDB instance was not deliberately stopped.
- Suggested fix: run production migration as a release step, or allow the host to listen while readiness remains unhealthy and retry initialization safely.

## GD2-DEF-03 — BA-blocked preview semantics can appear authoritative

- Severity: Medium
- Files: `src/Hosco.Application/Semantics/QueryCatalog.cs:20-30`; `src/Hosco.Api/Controllers/ReportingController.cs:37-72`; `src/Hosco.Infrastructure/Persistence/ReportingDataStore.cs`
- Problem: revenue, product-ranking, and dangerous-inventory query IDs are marked `BlockedByBusinessDefinition`, yet the endpoints return computed 200 responses without a definition-status field. Consumers can mistake provisional rules for approved KPI semantics.
- Evidence: catalog statuses and controller/data-store implementation were compared directly; runtime confirms the routes execute.
- Suggested fix: after BA approval, promote definitions with versioned formulas; until then expose explicit provisional status in each response or prevent business consumption.

## GD2-DEF-04 — Shared demo credential is tracked in multiple files

- Severity: Low
- Files: locations listed in `SECRET_AUDIT.md`
- Problem: the credential is documented as development-only, and only hashes are stored in SQL, but the shared input value is present in source/tests/examples and will trigger secret scanners or be reused accidentally.
- Evidence: location-only tracked-file scan; no value is reproduced here.
- Suggested fix: inject a seed/test password through environment or test configuration and keep only a non-secret placeholder in tracked examples.

## GD2-DEF-05 — Login query emitted a multiple-collection include warning

- Severity: Low
- File: `src/Hosco.Infrastructure/Security/IdentityStore.cs:11-14`
- Problem: login loads roles and branches as two collection includes using the default single-query behavior, which can create a cartesian product as assignments grow.
- Evidence: EF Core emitted `MultipleCollectionIncludeWarning` during SQL-backed API runtime verification.
- Suggested fix: use a projection or explicitly evaluated split-query strategy after measuring the workload.

