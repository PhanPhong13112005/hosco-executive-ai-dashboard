# Semantic and Query Catalog Verification

## Metric catalog

Eight definitions exist: KPI-01 revenue, KPI-02 GMV, KPI-03 total orders, KPI-04 AOV, KPI-05 gross profit/margin, KPI-06 cancellation/return rate, KPI-07 top/bottom SKU, and KPI-08 dangerous stock. Each definition contains code, name, description, unit, supported filters/dimensions, version `1.0`, query ID, status, and blocker.

All eight are `BlockedByBusinessDefinition`; this audit does not mark their business formulas as verified.

## Query allow-list

| Query ID | Catalog status |
|---|---|
| `orders.list.v1` | Implemented |
| `kpis.summary.v1` | ProvisionalTechnicalPreview |
| `revenue.summary.v1` | BlockedByBusinessDefinition |
| `revenue.trend.v1` | BlockedByBusinessDefinition |
| `gmv.summary.v1` | BlockedByBusinessDefinition |
| `orders.summary.v1` | BlockedByBusinessDefinition |
| `aov.summary.v1` | BlockedByBusinessDefinition |
| `products.ranking.v1` | BlockedByBusinessDefinition |
| `inventory.dangerous.v1` | BlockedByBusinessDefinition |
| `gross-profit.summary.v1` | BlockedByBusinessDefinition |
| `cancel-return-rate.summary.v1` | BlockedByBusinessDefinition |

The catalog stores allowed roles, parameters, output type, and version `1`. Unknown query IDs throw instead of accepting arbitrary SQL or identifiers.

`ReportingFilter.Validate` enforces page >= 1, page size 1..200, `from <= to`, and sort direction `asc`/`desc`. Date/branch filters are documented across metric definitions; query-specific parameter arrays are present in the query catalog.

## BA-pending rules

Revenue recognition statuses/refunds/discounts, GMV included statuses, total-order statuses, AOV numerator/denominator, gross-profit return allocation, cancellation/return denominator, ranking basis, and dangerous-stock threshold source are not approved. Data-store preview computations exist, but are not evidence of approved formulas. Individual revenue/ranking/inventory responses do not carry the catalog status; see the semantic contract defect in `DEFECTS_FOUND.md`.

Verdict: `PARTIALLY VERIFIED`: catalog, allow-list, versioning, and validation exist and tests pass; business semantics remain BA-blocked.

