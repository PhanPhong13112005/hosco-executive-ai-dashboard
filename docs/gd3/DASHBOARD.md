# Executive Dashboard GD3

Dashboard hiển thị đúng tám KPI Final GD1: Revenue, GMV, Total Orders, AOV, Gross Profit/Margin, combined Cancellation/Return Rate, Top/Bottom 10 SKU theo valid quantity và Dangerous Stock theo Available. Filter ngày được gửi với offset UTC+7; UI hiển thị `lastUpdatedAt` theo UTC+7 và cảnh báo rõ khi `isStale=true` (quá 30 phút).

Các khu vực UI gồm header/filter, KPI cards, Revenue Trend, Alert attention, inventory với OnHand/Reserved/Available/Safety, Top/Bottom 10 và drill-down. Drill-down hiển thị KPI/value/trend/scope/time và nút quay lại Dashboard. Loading/empty/error/403 đều có state riêng. Trợ lý AI chỉ là entry disabled “GD4”.

| Khu vực | Endpoint |
|---|---|
| Summary | `GET /api/v1/reporting/dashboard/summary` |
| Revenue Trend | `GET /api/v1/reporting/revenue/trend` |
| Top/Bottom 10 | `GET /api/v1/reporting/products/top|bottom?pageSize=10` |
| Dangerous Stock | `GET /api/v1/reporting/inventory/dangerous` |
| Drill-down | `GET /api/v1/reporting/kpis/{metricId}/drilldown` |
| Alert attention | `GET /api/v1/alerts` |

UI không hiển thị DedupKey như field business chính; key chỉ nằm trong technical details của Alert Detail.
