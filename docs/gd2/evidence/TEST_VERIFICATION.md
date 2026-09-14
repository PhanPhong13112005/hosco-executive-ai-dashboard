# Test Verification

## Executed result

`dotnet test` and `dotnet test Hosco.slnx --no-build --logger "trx;LogFileName=gd2-tests.trx"` both completed successfully.

| Suite | Passed | Failed | Skipped | Total | Runner-reported duration |
|---|---:|---:|---:|---:|---:|
| `Hosco.UnitTests` | 11 | 0 | 0 | 11 | 58 ms on TRX run |
| `Hosco.IntegrationTests` | 17 | 0 | 0 | 17 | 1 s on TRX run |
| Total | 28 | 0 | 0 | 28 | Per-suite values above |

TRX files were generated under each test project's ignored `TestResults/` directory and are intentionally not evidence artifacts.

## Source-to-requirement coverage

| Area | Test method(s) / cases | Evidence |
|---|---|---|
| Tenant isolation | `Cross_tenant_branch_is_always_forbidden`; `Cross_tenant_branch_is_forbidden`; `Reporting_never_leaks_other_tenant_data`; `Client_tenantId_query_parameter_cannot_override_claim` | Unit and real HTTP pipeline |
| Branch scope | `Branch_manager_can_access_assigned_branch`; `Branch_manager_cannot_access_unassigned_branch`; `Chain_manager_can_access_any_branch_in_same_tenant`; HTTP equivalents for assigned, unassigned, and chain-manager access | Unit and integration |
| Auth | `Reporting_without_login_returns_401`; all authenticated endpoint tests obtain a JWT through `/api/v1/auth/login` | Integration |
| Reporting API | `Reporting_contract_endpoints_execute` has four route cases; order retrieval tests; `Invalid_filter_returns_400`; `OpenApi_document_is_available_when_enabled` | Integration |
| Seed dataset | `Seed_exposes_six_month_deterministic_tenant_dataset` verifies 1,042 tenant-A orders and first timestamp 2026-01-01 08:00 UTC | Integration |
| Semantic/query catalog | `Metric_catalog_contains_all_eight_kpis_and_marks_unapproved_definitions`; `Query_catalog_rejects_non_allowlisted_query` | Unit |
| Filter validation | `Pagination_rejects_out_of_range_values` has three cases; `Filter_rejects_inverted_date_range`; `Filter_accepts_maximum_page_size` | Unit |
| Health | `Health_endpoint_is_healthy` covers `/health/live` and `/health/ready` | Integration |
| Correlation | `Valid_correlation_id_is_reused`; 401 test also asserts the response header exists | Integration |

Limit: tests do not exercise SQL Server-specific query translation; their API fixture uses relational SQLite in-memory. SQL Server was therefore verified separately in the runtime and database checks.

Verdict: `VERIFIED` for the executed suite; coverage limitations remain documented.

