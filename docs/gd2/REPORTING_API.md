# Reporting API v1

Base path: `/api/v1`. Reporting endpoints require Bearer JWT and the `ReportingReader` policy.

| Method/path | Query ID | Inputs | Result |
|---|---|---|---|
| `POST /auth/login` | n/a | email, password | JWT and expiry |
| `GET /reporting/kpis/summary` | `kpis.summary.v1` | date/branch filter | all KPI contracts plus definition status |
| `GET /reporting/revenue` | `revenue.trend.v1` | date/branch filter | daily currency amounts (preview) |
| `GET /reporting/orders` | `orders.list.v1` | date/branch, page, pageSize, sort | paged order rows |
| `GET /reporting/products/ranking` | `products.ranking.v1` | date/branch, `bottom`, pageSize | ranked product rows (preview) |
| `GET /reporting/inventory/dangerous` | `inventory.dangerous.v1` | branch, pageSize | inventory below/equal safety stock (preview) |
| `GET /health/live` | n/a | none | process health |
| `GET /health/ready` | n/a | none | database readiness |

Common filter: `from`, `to`, `branchId`, `page` (default 1), `pageSize` (default 50, max 200), `sortBy`, `sortDirection`. Supported order sorts are `orderedAt`, `orderNumber`, and `amount`; unknown values fall back to newest-first. `TenantId` is not an input.

Successful reporting responses use `{ data, meta }`; metadata includes the effective date range, requested branch, `lastUpdatedAt`, `isStale`, `queryId`, `correlationId`, and pagination where applicable. Error middleware returns 400/403/404/409/500 with a stable code/message/correlation ID. Authentication middleware returns 401 for missing or invalid JWTs.

Swagger/OpenAPI documents request/response models and Bearer authentication at `/swagger` in Development. The future Dashboard and Chatbot must use these contracts; neither may issue arbitrary SQL.
