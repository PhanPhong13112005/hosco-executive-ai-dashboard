# Kiến trúc GD3

## Tổng quan

GD3 mở rộng kiến trúc bốn layer của GD2, không tạo đường tắt từ Web UI tới database.

```mermaid
flowchart LR
    Web[React / TypeScript Web UI] --> API[ASP.NET Core API v1]
    API --> Auth[JWT + RBAC]
    Auth --> Scope[Tenant / Branch Scope]
    Scope --> Reporting[ReportingDataStore]
    Scope --> AlertService[AlertService]
    Scheduler[BackgroundService 15-30 phút] --> Engine[AlertEngine]
    Engine --> E1[AL-01 Evaluator]
    Engine --> E2[AL-02 Evaluator]
    Engine --> E3[AL-03 Evaluator]
    Engine --> E4[AL-04 Evaluator]
    Engine --> E5[AL-05 Evaluator]
    E1 & E2 & E3 & E4 & E5 --> Signals[AlertSignalDataStore]
    Engine --> Dedup[Dedup + Cooldown]
    Dedup --> Repo[AlertRepository]
    Repo --> DB[(SQL Server / EF Core)]
    Engine --> Notify[INotificationSender]
    Notify --> Log[LoggingNotificationSender]
```

## Ranh giới layer

- `Hosco.Domain`: `AlertRule`, `Alert`, enum status/severity và entity thương mại GD2.
- `Hosco.Application`: typed contract, evaluator Strategy, engine orchestration, workflow service, `TimeProvider` và abstraction persistence/notification.
- `Hosco.Infrastructure`: EF mapping/repository, signal query, seed và Migration.
- `Hosco.Api`: controller, authorization policy, scheduler hosted service, structured logging và composition root.
- `Hosco.Web`: API client tập trung, page/component/type; không tham chiếu database.

## Scope và security

Tenant không xuất hiện trong request filter. `ReportingScopeFactory` suy ra Tenant từ JWT và giới hạn BranchManager vào branch claim. Alert list/detail sử dụng cùng `ReportingScope`; tài nguyên ngoài scope trả 404 để không lộ sự tồn tại. Thay đổi Alert Rule yêu cầu `Owner`, `ChainManager` hoặc `SystemAdmin`; `SystemAdmin` vẫn bị giới hạn Tenant.

## Thời gian và khả năng kiểm thử

Evaluator nhận `now` do `AlertEngine` lấy từ `TimeProvider`, không gọi `DateTime.Now`. Scheduler tạo DI scope riêng cho mỗi chu kỳ. Lỗi một rule được ghi log qua `IAlertEngineDiagnostics`; các rule còn lại tiếp tục.

