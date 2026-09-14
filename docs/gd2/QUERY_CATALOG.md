# Query Catalog

All entries are versioned allow-list identifiers. Filters are represented as typed LINQ expressions and sorting uses a server-side allow-list. Every implementation accepts `CancellationToken` and executes under a server-derived `ReportingScope`.

| QueryId | KPI | Input/filter | Output | Roles | Version | Status |
|---|---|---|---|---|---|---|
| `revenue.summary.v1` | KPI-01 | from, to, branchId | revenue summary | all reporting roles | 1 | BlockedByBusinessDefinition |
| `revenue.trend.v1` | KPI-01 | from, to, branchId | `RevenuePoint[]` | all reporting roles | 1 | technical preview; BA rule pending |
| `gmv.summary.v1` | KPI-02 | from, to, branchId | KPI value | all reporting roles | 1 | BlockedByBusinessDefinition |
| `orders.summary.v1` | KPI-03 | from, to, branchId | KPI value | all reporting roles | 1 | BlockedByBusinessDefinition |
| `orders.list.v1` | none | range, branch, page/sort | paged `OrderRow` | all reporting roles | 1 | Implemented |
| `aov.summary.v1` | KPI-04 | from, to, branchId | KPI value | all reporting roles | 1 | BlockedByBusinessDefinition |
| `gross-profit.summary.v1` | KPI-05 | from, to, branchId | KPI value | all reporting roles | 1 | BlockedByBusinessDefinition |
| `cancel-return-rate.summary.v1` | KPI-06 | from, to, branchId | KPI value | all reporting roles | 1 | BlockedByBusinessDefinition |
| `products.ranking.v1` | KPI-07 | range, branch, bottom, pageSize | `ProductRankRow[]` | all reporting roles | 1 | technical preview; ranking basis pending |
| `inventory.dangerous.v1` | KPI-08 | branch, pageSize | `DangerousInventoryRow[]` | all reporting roles | 1 | technical preview; threshold rule pending |
| `kpis.summary.v1` | KPI-01..08 | from, to, branchId | `KpiValue[]` | all reporting roles | 1 | ProvisionalTechnicalPreview |

The in-code catalog exposes every identifier above. Blocked entries are contracts only; they cannot become approved handlers until their BA formula and semantic version are confirmed.
