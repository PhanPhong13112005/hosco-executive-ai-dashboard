# Kiến trúc GD2

## Sơ đồ thành phần

```mermaid
flowchart TD
    Web[Web / Dashboard - GD3] --> API[Reporting API v1]
    Bot[Future Chatbot - GD4] --> API
    API --> Auth[JWT + RBAC + Tenant/Branch Scope]
    API --> Semantic[Semantic Layer / Metric Catalog]
    API --> Catalog[Allow-list Query Catalog]
    Semantic --> Store[Parameterized EF Core Reporting Store]
    Catalog --> Store
    Store --> DB[(SQL Server)]
    API --> Audit[(AuditLog)]

    Bot -. prohibited .-> NoDB[No direct database access]
    NoDB -.x DB
```

## Chiều phụ thuộc

`Hosco.Api -> Hosco.Application -> Hosco.Domain`; `Hosco.Infrastructure` hiện thực các abstraction của Application và phụ thuộc vào Domain/Application. Domain không phụ thuộc framework.

## Luồng xử lý request

1. Correlation middleware kiểm tra/tái sử dụng `X-Correlation-ID` hoặc tạo giá trị mới.
2. JWT middleware xác thực, sau đó policy authorization kiểm tra vai trò được phép đọc báo cáo.
3. `CurrentUser` cung cấp tập trung `UserId`, `TenantId`, các vai trò và branch claim.
4. `ReportingScopeFactory` từ chối Branch thuộc Tenant khác và vẫn giới hạn Branch Manager khi không truyền `branchId`.
5. Reporting handler có version, nằm trong allow-list, thực thi truy vấn EF Core với điều kiện Tenant và Branch bắt buộc.
6. Response có metadata về query, correlation và thời điểm cập nhật gần nhất; truy vấn báo cáo nhạy cảm có thể tạo `AuditLog`.

## Ranh giới mở rộng

- Chỉ bổ sung KPI handler đã được phê duyệt phía sau `IReportingDataStore` và cập nhật catalog sau khi BA chấp thuận.
- Alert Engine ở GD3 có thể dùng cùng catalog và lưu `Alert`; thao tác acknowledge/resolve có thể dùng `IAuditWriter`.
- AI Orchestrator ở GD4 nhận token và gọi `/api/v1/reporting/...`; thành phần này không được nhận connection string cơ sở dữ liệu hoặc khả năng thực thi SQL tùy ý.
- Dịch vụ bên ngoài có thể bổ sung retry policy tại ranh giới typed HTTP client; retry SQL hiện tại được giới hạn tối đa ba lần.
