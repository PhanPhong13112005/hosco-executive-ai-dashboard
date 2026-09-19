# Kế hoạch triển khai GD2

## 1. Trạng thái repository

- Workspace ban đầu chỉ có thư mục làm việc/output, chưa có source code hoặc tài liệu dự án.
- Không phát hiện Git repository (`git status` trả về `not a git repository`).
- Không phát hiện tài liệu BA (SRS v1, KPI Dictionary, Vai trò/Phân quyền, Luồng nghiệp vụ, Conversation Design, Intent Dataset).
- Máy có .NET SDK 10.0.400 và ASP.NET Core Runtime 10.0.11; đây là toolchain ổn định duy nhất có SDK đầy đủ tại thời điểm audit.

## 2. Công nghệ và quyết định kỹ thuật

- ASP.NET Core Web API / C# / .NET 10.
- Entity Framework Core 10, SQL Server provider cho production/development và SQLite in-memory cho Integration Test.
- JWT Bearer, policy authorization, `CurrentUserContext`, dịch vụ phân quyền theo Tenant/Branch.
- OpenAPI/Swagger cho môi trường Development.
- Kiến trúc bốn project: Domain, Application, Infrastructure, Api; hai project test.
- Seed generator xác định thay vì commit file SQL lớn.

## 3. Thứ tự triển khai

1. Tạo solution, project reference, cấu hình package và baseline repository.
2. Tạo domain model, `DbContext`, mapping, Migration đầu tiên và Seed generator xác định.
3. Tạo auth demo, JWT, RBAC và phạm vi Tenant/Branch không nhận `TenantId` từ client.
4. Tạo semantic contract, metric catalog, Query Catalog và validation.
5. Tạo Reporting API v1, response metadata, pagination/sort/filter và truy vấn được parameterize qua EF Core.
6. Tạo Correlation ID, structured request logging, global exception handling và audit baseline.
7. Tạo live/ready Health Check, timeout/cancellation/config/secrets baseline.
8. Cấu hình OpenAPI/Swagger và tài liệu endpoint.
9. Viết Unit Test/Integration Test, chạy restore/build/test, kiểm tra Migration/Seed/API/security.
10. Hoàn thiện README và toàn bộ tài liệu `docs/gd2`, gồm báo cáo hoàn thành.

## 4. Dependency

- .NET SDK 10.0.400.
- NuGet package: EF Core SQL Server/SQLite/Design, JWT Bearer, OpenAPI/Swagger, xUnit, ASP.NET Core MVC Testing.
- SQL Server là dependency Runtime chính; Integration Test dùng SQLite để chạy độc lập nhưng vẫn kiểm tra hành vi quan hệ.

## 5. Rủi ro và blocker (đã được Final GD1 cập nhật)

- Bối cảnh trên đúng tại GD2 ban đầu. Final GD1 hiện đã chốt cả tám công thức KPI; catalog/handler đã được nâng lên version 2.0 `Implemented` và dùng chung canonical layer.
- Các giá trị số của Smart Alert vẫn là BA đề xuất/configurable; không được nâng thành business truth bất biến.
- SQL Server cục bộ có thể chưa được cài đặt/khởi chạy; Migration có thể được tạo và kiểm tra bằng EF tooling, còn việc apply lên SQL Server phụ thuộc connection string và máy chạy.
- NuGet restore có thể cần kết nối mạng nếu package chưa có trong cache.

## 6. Tài liệu BA tìm thấy

Không tìm thấy tài liệu BA nào trong workspace tại thời điểm audit. Tài liệu kỹ thuật sẽ ghi rõ các điểm cần đối chiếu khi BA cung cấp bản mới nhất và sẽ không ghi đè Business Data Dictionary trong tương lai.
