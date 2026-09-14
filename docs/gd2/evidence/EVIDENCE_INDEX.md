# GD2 Evidence Index

Audited repository: `D:\\Code\\hosco-executive-ai-dashboard`  
Branch: `chore/gd2-evidence`  
Checkpoint: `f75ac38c323ac2b61dd127081cc742852200f197` (`gd2-complete`)  
Audit date: 2026-09-15 (Asia/Saigon)

Status vocabulary in this index is restricted to `VERIFIED`, `PARTIALLY VERIFIED`, `NOT VERIFIED`, and `NOT RUN`.

| GD2 Item | Evidence in Source | Runtime Evidence | Status |
|---|---|---|---|
| GD2-01 Repository & project structure | `Hosco.slnx`, six project files, project references, `src/`, `tests/`, `docs/gd2/` | Restore/build completed for all six projects; Git branch/tag/commit checked | VERIFIED |
| GD2-02 ERD / data dictionary / seed dataset | `ERD.md`, `DATA_DICTIONARY.md`, 16 `DbSet`s, initial migration, deterministic `DemoSeed` | Migration applied to SQL Server LocalDB; SQL artifact generated; seeded counts queried | PARTIALLY VERIFIED |
| GD2-03 Auth / tenant / branch scope | JWT configuration, `CurrentUser`, `IdentityStore`, `BranchScopeValidator`, `ReportingScopeFactory` | 401, 403, authenticated request, cross-tenant and query-parameter isolation tests passed | VERIFIED |
| GD2-04 Semantic layer / query catalog | Eight metric definitions, 11 allow-listed query IDs, version and filter metadata | Catalog/filter tests passed; only `orders.list.v1` is implemented and KPI formulas remain BA-pending | PARTIALLY VERIFIED |
| GD2-05 Observability / audit baseline | Correlation and exception middleware, JSON logging, `AuditLog`, `AuditWriter` | Correlation reuse test passed; runtime reporting request created an audit row | PARTIALLY VERIFIED |
| GD2-06 Secrets / env / health | `.gitignore`, `.env.example`, settings files, live/ready registrations | SQL-backed live and ready returned 200; scan found no identified production credential, but tracked demo credential material requires review | PARTIALLY VERIFIED |
| GD2-07 Reporting API v1 | `AuthController`, `ReportingController`, `ReportingDataStore`, OpenAPI setup | SQL-backed API: login 200, reporting 200, unauthenticated 401, out-of-scope 403; OpenAPI fetched | VERIFIED |

## Project structure and references

| Layer/project | Verified role | Direct project references |
|---|---|---|
| `Hosco.Domain` | Entities, common base types, enums | None |
| `Hosco.Application` | Interfaces, report models, scope services, semantic/query catalogs | `Hosco.Domain` |
| `Hosco.Infrastructure` | EF Core mappings/migration/seed, reporting and identity stores, password verification, audit writer | `Hosco.Domain`, `Hosco.Application` |
| `Hosco.Api` | HTTP controllers, JWT/authorization, middleware, health, composition root, Swagger | `Hosco.Application`, `Hosco.Infrastructure` |
| `Hosco.UnitTests` | Catalog, filter, and branch-scope unit tests | `Hosco.Application`, `Hosco.Domain` |
| `Hosco.IntegrationTests` | Real child-process HTTP API with relational SQLite in-memory database | `Hosco.Api` (`ReferenceOutputAssembly=false`, `Private=false`) |

## Detailed evidence

- [Build verification](BUILD_VERIFICATION.md)
- [Test verification](TEST_VERIFICATION.md)
- [Database verification](DATABASE_VERIFICATION.md)
- [Seed verification](SEED_VERIFICATION.md)
- [Security verification](SECURITY_VERIFICATION.md)
- [Semantic/query verification](SEMANTIC_QUERY_VERIFICATION.md)
- [API verification](API_VERIFICATION.md)
- [Observability and audit verification](OBSERVABILITY_AUDIT_VERIFICATION.md)
- [Secret audit](SECRET_AUDIT.md)
- [Dependency audit](DEPENDENCY_AUDIT.md)
- [Defects found](DEFECTS_FOUND.md)
- [Consolidated report](GD2_EVIDENCE_REPORT.md)
- [Generated SQL](sql/gd2-schema.sql)
- [Captured OpenAPI](openapi/gd2-openapi.json)

