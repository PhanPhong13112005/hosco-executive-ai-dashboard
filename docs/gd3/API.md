# API GD3

Base path `/api/v1`, Bearer JWT, Tenant/Branch scope server-side.

## Reporting

`/reporting/dashboard/summary`, `/revenue/trend`, `/orders/trend`, `/products/top`, `/products/bottom`, `/inventory/dangerous`, `/kpis/{metricId}/drilldown`, `/branches`. Summary trả `revenue`, `gmv`, `totalOrders`, nullable `aov`, `grossProfit`, nullable `grossMarginPercent`, `cancellationReturnRate`, dangerous/Alert counts. Ngày nghiệp vụ là UTC+7.

## Alert workflow

- `GET /alerts`, `GET /alerts/{id}`: list/detail trong scope.
- `POST /alerts/{id}/acknowledge`: lưu actor/time.
- `POST /alerts/{id}/resolve` với `{ "note": "..." }`: note optional trừ AL-04/AL-05 bắt buộc, tối đa 2.000 ký tự.
- AL-04 chỉ BranchManager trong assigned Branch scope được acknowledge/resolve; vai trò khác nhận 403 dù có quyền Alert nền.
- Detail trả observed, threshold, baseline, context, scope, timestamps, actor, resolution note và `escalatedAt` để escalation chỉ phát một lần.

## Rule configuration

`GET /alert-rules`; `PATCH /alert-rules/{id}` hỗ trợ enabled, default severity, threshold, baseline, window, cooldown và typed-config JSON. Owner/ChainManager/SystemAdmin được phép theo scope; BranchManager nhận 403. Giá trị số được gắn nhãn BA đề xuất/configurable.

Ngoài scope trả 403 khi chọn Branch, hoặc 404 cho resource-by-ID để chống IDOR. Error trả `code/message/correlationId`.
