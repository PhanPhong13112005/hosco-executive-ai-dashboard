# Báo cáo triển khai GD3 – Dashboard & Smart Alert

## 1. Phạm vi đã hoàn tất

GD3 mở rộng trực tiếp nền tảng GD2 với Executive Dashboard MVP, Smart Alert Engine, scheduler, persistence, deduplication/cooldown, acknowledge/resolve workflow, notification abstraction, React Web UI, migration, seed, test và tài liệu. Không triển khai Chatbot/LLM hoặc Telegram/FCM production.

## 2. Kiến trúc

- Giữ chiều phụ thuộc `Api -> Application -> Domain`; Infrastructure triển khai các abstraction của Application.
- Dashboard tái sử dụng Reporting scope, Tenant/Branch authorization và SQL Server/EF Core hiện có.
- Mỗi rule Alert có evaluator riêng qua `IAlertRuleEvaluator`; engine dùng `TimeProvider`, repository và notification abstraction.
- `BackgroundService` tạo DI scope theo mỗi cycle, xử lý từng rule độc lập và ghi structured log khi rule lỗi.
- React/TypeScript gọi API qua centralized client; frontend không truy cập database trực tiếp.

## 3. Database

- Thêm bảng `AlertRules` với cấu hình Tenant/Branch, threshold, baseline, window, cooldown và config JSON.
- Mở rộng `Alerts` với liên kết rule, message/context/value, actor/timestamp workflow và deterministic `DedupKey`.
- Thêm các index phục vụ scope, status/severity, thời gian phát hiện và cooldown lookup.
- Tạo migration mới `20260915151000_AddDashboardAlertFoundation`; không thay đổi migration GD2 `InitialCreate`.
- Đã generate idempotent SQL tại `docs/gd3/evidence/sql/gd3-migration.sql`, apply LocalDB thành công và kiểm tra schema/rule seed.
- EF Core: không có model change chưa được migration.

## 4. Dashboard API

- `GET /api/v1/reporting/dashboard/summary`
- `GET /api/v1/reporting/revenue/trend`
- `GET /api/v1/reporting/orders/trend`
- `GET /api/v1/reporting/products/top`
- `GET /api/v1/reporting/products/bottom`
- `GET /api/v1/reporting/inventory/dangerous`
- `GET /api/v1/reporting/branches`
- `GET /api/v1/reporting/kpis/{metricId}/drilldown`

Các endpoint nhận filter thời gian/Branch phù hợp, dùng typed DTO, `CancellationToken`, JWT và Reporting scope hiện có. KPI chưa được BA duyệt được gắn `ProvisionalTechnicalPreview`/`PENDING`, không được trình bày như business truth.

## 5. Alert API và workflow

- `GET /api/v1/alerts` và `GET /api/v1/alerts/{id}`.
- `POST /api/v1/alerts/{id}/acknowledge`.
- `POST /api/v1/alerts/{id}/resolve`.
- `GET /api/v1/alert-rules` và `PATCH /api/v1/alert-rules/{id}`.
- Workflow hỗ trợ `Open -> Acknowledged -> Resolved` và `Open -> Resolved`; mutation lưu actor/time và ghi audit.
- Alert detail/mutation chống IDOR bằng Tenant/Branch scope. Cấu hình rule chỉ dành cho Owner, ChainManager và SystemAdmin.

## 6. Năm Smart Alert rule MVP

| Code | Rule | Trạng thái nghiệp vụ |
|---|---|---|
| AL-01 | Tỷ lệ hủy đơn bất thường | Configurable, BA `PENDING` |
| AL-02 | Doanh thu giờ cao điểm giảm so với baseline | Configurable, BA `PENDING` |
| AL-03 | SKU quan trọng có tồn kho không vượt safety stock | Configurable, BA `PENDING` |
| AL-04 | Nhân viên/thu ngân có số lượng hủy/hoàn bất thường | Configurable, BA `PENDING` |
| AL-05 | Giá bán hoặc tỷ lệ giảm giá bất thường | Configurable, BA `PENDING` |

Evaluator là deterministic và unit-testable. Threshold/baseline/window/cooldown thuộc cấu hình rule, không hard-code thành định nghĩa nghiệp vụ chính thức.

## 7. Scheduler, deduplication và notification

- Scheduler configurable, mặc định 15 phút và validate phạm vi 15–30 phút.
- Một rule lỗi không dừng các rule còn lại hoặc làm chết worker.
- `DedupKey` bao gồm Tenant, Branch, Rule và entity/context; repository kiểm tra cùng key trong cooldown trước khi persist.
- Alert sau cooldown có thể được tạo lại; khác Tenant/Branch không bị dedup nhầm.
- `LoggingNotificationSender` được gọi sau khi Alert mới persist; không chứa token/API key/secret.

## 8. Frontend

- Login demo và responsive application shell.
- Executive Dashboard: date/Branch filter, KPI cards, revenue/order trend, Alert highlights, dangerous stock, Top/Bottom SKU và KPI drill-down.
- Alert Center: summary, filters và danh sách severity/status/Branch/time.
- Alert Detail: context, acknowledge và resolve.
- Alert Configuration: xem/sửa năm rule với role guard.
- Có loading, empty, error và permission-denied state. Chatbot chỉ là placeholder disabled cho GD4.
- `npm run build`: PASS, TypeScript compile thành công, 24 modules được bundle.

## 9. Security

- Unauthenticated request trả `401`.
- Tenant isolation và Branch scope được áp dụng ở Reporting/Alert query và mutation.
- Branch ngoài phạm vi trả `403` theo convention hiện có.
- Alert ID ngoài scope không bị lộ qua detail/acknowledge/resolve.
- Rule configuration yêu cầu role phù hợp; không thêm secret vào source/config.

## 10. Build và test

- Baseline trước GD3: Build PASS; 11 Unit Test + 17 Integration Test GD2 = 28/28 PASS.
- Final restore: PASS, có cảnh báo môi trường `NU1900` do không đọc được NuGet vulnerability feed.
- Final build: PASS, 0 error, 1 cảnh báo `NU1900`.
- Unit Test: 24/24 PASS (11 GD2 + 13 GD3), 0 failed, 0 skipped.
- Integration Test: 26/26 PASS (17 GD2 + 9 GD3), 0 failed, 0 skipped trong lần chạy cuối.
- Tổng: 50/50 PASS.
- Runtime API: login/dashboard/Alert workflow PASS; unauthenticated `401`, Branch ngoài scope `403`.

## 11. DOTNET RUNTIME CHECK

- `dotnet --info`: SDK 10.0.400, MSBuild 18.9.6, x64 Host 10.0.11 trên Windows; `global.json` được nhận đúng.
- SDK: 10.0.400.
- Runtime dùng cho project: `Microsoft.NETCore.App` và `Microsoft.AspNetCore.App` 10.0.11.
- Build: PASS, 0 error; 1 cảnh báo môi trường `NU1900` do NuGet vulnerability feed không truy cập được.
- Tests: lần cuối 24/24 Unit Test và 26/26 Integration Test PASS.
- Event log: incident trước có `.NET Runtime` 1026 và `Application Error` 1000 (`0xe0434352`).
- Code Integrity: Event 3033/3077, HRESULT `0x800711C7`, policy `{0283ac0f-fff1-49ae-ada1-8a933130cad6}` chặn DLL local unsigned ở Enterprise signing level.
- Reproduced: có tái hiện trong lần incident trước; không tái hiện trong lần final validation.
- Conclusion: intermittent local WDAC/environment issue, không phải SDK/runtime installation defect hoặc application business-logic defect theo bằng chứng hiện có.

## 12. LOCAL ENVIRONMENT

- Build: PASS.
- Unit Tests: 24/24 PASS.
- Integration Tests: 26/26 PASS trong lần final validation; lần incident trước là `BLOCKED_BY_LOCAL_WDAC` và không được ghi PASS.
- WDAC status: enabled/không bị thay đổi; known intermittent blocking đối với DLL local unsigned.
- Runtime validation: unblocked trong lần final validation.

Một lần chạy trước bị Enterprise Windows Code Integrity chặn DLL build local chưa ký trước khi application startup: Event 1026/1000, Code Integrity 3033/3077 và HRESULT `0x800711C7`. Lần đó được ghi chính xác là `BLOCKED_BY_LOCAL_WDAC`, không phải Integration Test PASS và không phải business-logic defect.

Lần xác minh cuối trên cùng source chạy được đầy đủ Integration Test và runtime mà không disable WDAC, reinstall .NET hoặc sửa code để né policy. Vì vậy trạng thái hiện tại là runtime validation **unblocked cho lần chạy cuối**, còn WDAC vẫn là known intermittent environment issue. Chi tiết tại `KNOWN_ENVIRONMENT_ISSUES.md`.

## 13. BA blockers và giới hạn còn lại

- Revenue recognition, GMV status rules, total-order denominator, AOV, COGS/return allocation, cancellation/return denominator, SKU ranking basis và dangerous-stock threshold chờ BA duyệt.
- Các Alert threshold/baseline hiện là cấu hình demo, không phải business truth đã approved.
- Notification production, distributed scheduler/locking và multi-instance delivery chưa thuộc phạm vi MVP.
- Local enterprise policy có thể tiếp tục chặn DLL Debug chưa ký; không được khắc phục bằng cách tắt WDAC hoặc sửa application để né policy.
- Figma reference không truy cập được trong môi trường triển khai; UI bám wireframe mô tả, không cam kết pixel-perfect.

## 14. Kết luận

GD3 sẵn sàng cho code review/demo trong phạm vi MVP. Review cần giữ nhãn provisional cho KPI/Alert config đang chờ BA và lưu ý WDAC local có thể làm một lần runtime verification bị `BLOCKED_BY_LOCAL_WDAC`. Không có commit, push, merge hoặc thay đổi `main` được thực hiện trong quá trình triển khai.
