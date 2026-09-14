# GD2 Implementation Plan

## 1. Trạng thái repository

- Workspace ban đầu chỉ có thư mục làm việc/output, chưa có source code hay tài liệu dự án.
- Không phát hiện Git repository (`git status` trả về `not a git repository`).
- Không phát hiện tài liệu BA (SRS v1, KPI Dictionary, Vai trò/Phân quyền, Luồng nghiệp vụ, Conversation Design, Intent Dataset).
- Máy có .NET SDK 10.0.400 và ASP.NET Core Runtime 10.0.11; đây là toolchain ổn định duy nhất có SDK đầy đủ tại thời điểm audit.

## 2. Stack và quyết định kỹ thuật

- ASP.NET Core Web API / C# / .NET 10.
- Entity Framework Core 10, SQL Server provider cho production/development và SQLite in-memory cho integration test.
- JWT Bearer, policy authorization, `CurrentUserContext`, tenant/branch authorization service.
- OpenAPI/Swagger cho Development.
- Kiến trúc bốn project: Domain, Application, Infrastructure, Api; hai project test.
- Seed generator deterministic thay vì commit file SQL lớn.

## 3. Thứ tự triển khai

1. Tạo solution, project references, cấu hình package và baseline repository.
2. Tạo domain model, `DbContext`, mappings, migration đầu tiên và deterministic seed generator.
3. Tạo auth demo, JWT, RBAC và tenant/branch scope không nhận `TenantId` từ client.
4. Tạo semantic contracts, metric catalog, query catalog và validation.
5. Tạo Reporting API v1, response metadata, pagination/sort/filter và truy vấn đã parameterize qua EF Core.
6. Tạo correlation ID, structured request logging, global exception handling và audit baseline.
7. Tạo live/ready health checks, timeout/cancellation/config/secrets baseline.
8. Cấu hình OpenAPI/Swagger và tài liệu endpoint.
9. Viết unit/integration tests, chạy restore/build/test, kiểm tra migration/seed/API/security.
10. Hoàn thiện README và toàn bộ tài liệu `docs/gd2`, gồm completion report.

## 4. Dependency

- .NET SDK 10.0.400.
- NuGet packages: EF Core SQL Server/SQLite/Design, JWT Bearer, OpenAPI/Swagger, xUnit, ASP.NET Core MVC Testing.
- SQL Server là dependency runtime chính; integration tests dùng SQLite để có thể chạy độc lập và vẫn kiểm tra relational behavior.

## 5. Rủi ro và blocker

- Không có BA documents để xác nhận công thức KPI. Vì vậy các công thức chưa được chốt sẽ chỉ có contract/catalog/status `BlockedByBusinessDefinition`; không hard-code công thức đoán như business truth.
- Reporting handlers chỉ implement các phép tổng hợp có thể biểu diễn như technical preview; endpoint summary chỉ trả metric có status implemented. Các quyết định về refund/discount/status/ranking/cost/safety-stock cần BA xác nhận trước khi coi là production KPI.
- SQL Server cục bộ có thể chưa được cài/chạy; migration có thể được tạo và kiểm tra bằng EF tooling, còn apply lên SQL Server sẽ phụ thuộc connection string/máy người chạy.
- NuGet restore có thể cần network nếu package chưa có trong cache.

## 6. Tài liệu BA tìm thấy

Không tìm thấy tài liệu BA nào trong workspace tại thời điểm audit. Technical documentation sẽ ghi rõ các điểm cần đối chiếu khi BA cung cấp tài liệu mới nhất và sẽ không ghi đè Business Data Dictionary trong tương lai.

