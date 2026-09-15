# API GD3

Base path: `/api/v1`. Tất cả endpoint bên dưới yêu cầu Bearer JWT và có `CancellationToken` phía server.

## Dashboard/Reporting

| Method | Path | Mô tả |
|---|---|---|
| GET | `/reporting/dashboard/summary` | KPI card + Alert count, provisional |
| GET | `/reporting/revenue/trend` | Alias typed cho Revenue Trend GD2 |
| GET | `/reporting/orders/trend` | Tổng/hủy/hoàn theo ngày |
| GET | `/reporting/products/top` | Top SKU preview |
| GET | `/reporting/products/bottom` | Bottom SKU preview |
| GET | `/reporting/inventory/dangerous` | Dangerous Stock preview |
| GET | `/reporting/kpis/{metricId}/drilldown` | KPI hiện tại + trend |
| GET | `/reporting/branches` | Branch được user hiện tại phép đọc |

Các endpoint GD2 `/reporting/kpis/summary`, `/reporting/revenue`, `/reporting/orders`, `/reporting/products/ranking` vẫn giữ nguyên.

## Alert instances

| Method | Path | Mô tả |
|---|---|---|
| GET | `/alerts` | List + summary; filter Branch/severity/status/date/page |
| GET | `/alerts/{id}` | Alert detail trong scope |
| POST | `/alerts/{id}/acknowledge` | Open → Acknowledged; lưu actor/time |
| POST | `/alerts/{id}/resolve` | Open/Acknowledged → Resolved; lưu actor/time |

Tài nguyên khác Tenant/Branch scope trả 404 để chống IDOR. Request thiếu JWT trả 401.

## Alert rules

| Method | Path | Role |
|---|---|---|
| GET | `/alert-rules` | Mọi ReportingReader trong scope |
| PATCH | `/alert-rules/{id}` | Owner, ChainManager, SystemAdmin |

Payload PATCH hỗ trợ `isEnabled`, `severity`, `threshold`, `baseline`, `windowMinutes`, `cooldownMinutes`, `configJson`. Validation ngăn giá trị âm, window ngoài 1..43,200 phút, cooldown ngoài 0..43,200 phút và config JSON quá 4,000 ký tự.

## Error convention

401/403 do authentication/authorization middleware; 400 validation; 404 không tìm thấy/tránh IDOR; 500 lỗi ngoài dự kiến. Response lỗi có `code`, `message`, `correlationId` theo baseline GD2.

