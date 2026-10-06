# GD4 Reporting API Mapping

CHATBOT DOES NOT ACCESS DATABASE DIRECTLY.

All operations below are authenticated HTTP GET requests from a fixed enum allowlist. The public chat endpoint is `POST /api/v1/chat/messages`, authenticated by `ReportingReader`. User input cannot supply a URL/query ID or a tenant scope.

| Intent / purpose | Operation | Existing API path | Catalog query |
|---|---|---|---|
| Overview, Revenue, GMV, TotalOrders, AOV, GrossProfit, GrossMargin, CancellationReturnRate | DashboardSummary | `/api/v1/reporting/dashboard/summary` | `dashboard.summary.v1` |
| RevenueTrend | RevenueTrend | `/api/v1/reporting/revenue/trend` | `revenue.trend.v1` |
| TopProducts | TopProducts | `/api/v1/reporting/products/top` | `products.ranking.v1` |
| BottomProducts | BottomProducts | `/api/v1/reporting/products/bottom` | `products.ranking.v1` |
| DangerousInventory | DangerousInventory | `/api/v1/reporting/inventory/dangerous` | `inventory.dangerous.v1` |
| CurrentAlerts | AlertList | `/api/v1/alerts` | `alerts.list.v1` |
| Resolve accessible branch code/name | Branches | `/api/v1/reporting/branches` | API metadata |

Allowed parameters: from/to (UTC ISO timestamp), branchId resolved from scoped branch API, pageSize, supported severity. Alerts additionally use fixed `status=Open`. Query values are URI-escaped. All business scope is ultimately reconstructed by existing authenticated endpoints from JWT/current user, never from conversation context.

Reporting responses use `{data, meta}` including queryId; alert list uses its existing `{items,totalCount,...}` shape. The composer consumes those structured fields without recalculating canonical metrics. Trend answer reports returned point count and last point; it does not invent aggregate/comparison values. Ranking order and stock quantities remain those supplied by the API.

New business reporting endpoints were not necessary. Only the catalog description of the existing alert list read boundary was added. Dashboard export endpoints remain outside the chat allowlist; they were separately smoke-tested as GD3 regression.

## Request/response contract

```json
{"message":"Doanh thu hôm nay?","context":{"previousIntent":"Revenue","previousMetric":"revenue"}}
```

Response fields: message, intent, status, confidence, structured data, suggestions, context, reportingOperation and correlationId. Resolved requests return Completed; no-data is still a completed API read with a truthful empty-data message. Ambiguous/Unknown/Unavailable do not invent data.

See [runtime evidence](evidence/logs/chat-api-runtime.txt) for actual requests, responses, mappings, 401/403 and export results. Tokens are deliberately omitted.
