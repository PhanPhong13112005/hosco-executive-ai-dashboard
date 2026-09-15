# Kế hoạch triển khai GD3 – Dashboard & Smart Alert

## 1. Phạm vi

GD3 tiếp tục trực tiếp nền tảng GD2 trên `feature/gd3-dashboard-alert`: bổ sung Executive Dashboard MVP, Smart Alert Engine với năm rule cấu hình được, scheduler, persistence, deduplication/cooldown, workflow acknowledge/resolve, notification abstraction, React Web UI, kiểm thử và tài liệu. GD3 không triển khai Chatbot/LLM hoặc kênh Telegram/FCM production.

Baseline đã xác minh ngày 2026-09-15: Restore PASS, Build PASS (0 warning/error), 28/28 test GD2 PASS khi Integration Test được cấp quyền loopback/Data Protection ngoài sandbox.

## 2. Kiến trúc

- Giữ chiều phụ thuộc `Api -> Application -> Domain`, `Infrastructure -> Application/Domain`.
- Dashboard tái sử dụng `IReportingDataStore`, `ReportingScopeFactory`, Query Catalog và API v1; UI chỉ gọi HTTP API.
- Alert dùng `IAlertRuleEvaluator` theo Strategy, `IAlertEvaluationDataStore`, `IAlertRepository`, `IAlertEngine`, `TimeProvider` và `INotificationSender`.
- Scheduler dùng `BackgroundService`, tạo scope DI theo mỗi chu kỳ, cô lập lỗi từng rule và ghi structured log.
- Tenant lấy từ identity/rule, Branch luôn được kiểm tra bởi scope hiện có; không nhận TenantId từ client.

## 3. Database changes

- Mở rộng `Alert` với Rule, actor/timestamp workflow, value/context và `DedupKey`.
- Thêm `AlertRule` chứa Code/Name/Description/Severity/IsEnabled/Threshold/Baseline/Window/Cooldown/Tenant/Branch/Config.
- Tạo Migration mới `AddDashboardAlertFoundation`; tuyệt đối không sửa `20260914161316_InitialCreate`.
- Index phục vụ Tenant, Branch, Status, Severity, RuleId, DetectedAt và DedupKey/cooldown lookup.
- Seed năm rule AL-01..AL-05 theo từng Tenant và giữ anomaly fixture GD2 có tính xác định.

## 4. API

- Bổ sung dashboard aggregate và KPI drill-down, vẫn giữ nguyên các endpoint Reporting API GD2.
- Bổ sung Alert API: list/detail/acknowledge/resolve; Alert Rule API: list/update cấu hình.
- Contract typed, validation, `CancellationToken`, audit cho mutation; lỗi 401/403/404 theo convention hiện tại.
- Chỉ Owner/ChainManager/SystemAdmin được cập nhật cấu hình; BranchManager chỉ đọc dữ liệu trong Branch được phân công.

## 5. Frontend

- Tạo `src/Hosco.Web` bằng React + TypeScript + Vite, API client tập trung.
- Màn hình: Login demo, Executive Dashboard, KPI Drill-down, Alert Center, Alert Detail, Alert Configuration.
- Có loading/empty/error/permission denied; responsive; Chatbot chỉ là placeholder disabled cho GD4.
- Figma không đọc được qua công cụ truy cập hiện tại, nên UI bám mô tả wireframe trong yêu cầu.

## 6. Alert flow

`Scheduler -> enabled rules -> evaluator -> candidate -> deterministic DedupKey -> cooldown check -> persist Alert -> notification abstraction`.

Workflow instance: `Open -> Acknowledged -> Resolved`; cho phép `Open -> Resolved`. Mutation lưu user và timestamp từ `TimeProvider`.

## 7. Test strategy

- Giữ nguyên 28 test GD2.
- Unit Test: năm evaluator, boundary, TimeProvider/window, dedup/cooldown, transition và validation/quyền service.
- Integration Test: dashboard/auth/filter/Tenant/Branch; Alert list/detail/workflow/config/401/403/isolation; scheduler/engine không duplicate.
- Xác minh Migration SQL/LocalDB, Runtime API và frontend production build.

## 8. BA blockers

Revenue recognition, GMV statuses, total-order denominator, AOV, COGS/return allocation, cancellation/return denominators, SKU ranking basis và dangerous-stock threshold chưa được BA phê duyệt. Dashboard tiếp tục gắn nhãn `ProvisionalTechnicalPreview`/`PENDING`; Alert thresholds/baselines là cấu hình demo, không phải business truth.

## 9. Execution order

1. Domain, persistence và Migration GD3.
2. Dashboard reporting backend và drill-down.
3. Alert evaluators, engine, deduplication/cooldown.
4. Scheduler và notification abstraction.
5. Alert API/workflow/config và seed.
6. Dashboard/Alert Web UI.
7. Unit, Integration, security test.
8. Tài liệu, Migration/LocalDB/Runtime/frontend/final Git validation.
