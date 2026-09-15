# Báo cáo hoàn thành GD2

## 1. Tổng quan

Đã hoàn thành nền tảng backend .NET 10 cho HOSCO theo kiến trúc Domain/Application/Infrastructure/API. Reporting API là ranh giới duy nhất cho Dashboard và Chatbot tương lai; không có đường Chatbot → Database hoặc khả năng thực thi SQL tùy ý.

## 2. File đã tạo/sửa

- Solution/project: `Hosco.slnx`, `global.json`, 4 project source, 2 project test, manifest `dotnet-ef` local.
- Domain: entity và enum cho tổ chức/xác thực, thương mại/tồn kho/đơn hàng/thanh toán/hoàn tiền, cảnh báo/audit.
- Application: contract current-user/scope, contract filter/response, metric/query catalog, quy tắc phạm vi Branch/báo cáo.
- Infrastructure: SQL Server `DbContext`, mapping/index/FK, Migration, Seed xác định, identity/password, reporting/audit store.
- API: JWT/auth controller, năm Reporting API endpoint, correlation/exception/request logging, Health Check, timeout, Swagger.
- Vận hành/tài liệu: `.gitignore`, `.env.example`, appsettings, ví dụ request, README và toàn bộ `docs/gd2`.

## 3. Database/ERD

Schema có 16 bảng chính: Tenant, Branch, AppUser, Role, UserRole, UserBranch, Customer, Employee, Product, Inventory, Order, OrderItem, Payment, Refund, Alert, AuditLog. Tenant FK và các index phục vụ báo cáo đã được tạo. `OrderItem.UnitCostAtSale` lưu snapshot giá vốn lịch sử. Mermaid ERD nằm trong `ERD.md`; các trường kỹ thuật nằm trong `DATA_DICTIONARY.md`.

Migration `20260914161316_InitialCreate` đã được sinh. `dotnet-ef migrations has-pending-model-changes` trả về “No changes”; script SQL Server idempotent được sinh thành công (19,503 byte). Việc apply lên SQL Server/LocalDB không được chạy vì máy hiện tại không có LocalDB instance khả dụng; lệnh chuẩn đã có trong README.

## 4. Dữ liệu mẫu (Seed Dataset)

Generator dùng GUID xác định được suy ra từ SHA-256 và mốc cố định từ 2026-01-01 đến 2026-06-30. Dataset gồm 2 Tenant, 4 Branch, 5 danh tính demo, 8 Product/Tenant, Customer, thu ngân, Inventory, 2,084 Order, 4,168 OrderItem, 2,084 Payment và các Refund fixture. Alert fixture đánh dấu đột biến hủy đơn, sụt giảm doanh thu, tồn kho thấp và giảm giá bất thường. Seed không lưu mật khẩu plaintext; mật khẩu demo được hash bằng PBKDF2-SHA256.

## 5. Xác thực và phạm vi Tenant/Branch

JWT xác minh issuer/audience/signature/lifetime và chứa `sub`, role, `tenant_id`, `branch_id`. `CurrentUser` là nơi duy nhất phân tích claim. `ReportingScopeFactory` lấy Tenant từ identity, xác minh Branch thuộc Tenant, áp dụng phạm vi được phân công cho Branch Manager kể cả khi bỏ `branchId`, đồng thời vẫn giới hạn System Admin trong Tenant.

Integration Test xác nhận request chưa xác thực trả 401, Branch hợp lệ, Branch ngoài quyền trả 403, Tenant khác trả 403, Chain Manager truy cập trong Tenant của mình, không rò rỉ Tenant khi bỏ Branch và query parameter `tenantId` không ghi đè claim.

## 6. Lớp ngữ nghĩa (Semantic Layer)

Metric catalog có KPI-01 đến KPI-08 với code/name/description/unit/filters/dimensions/version/query ID/status/blocker. Vì không có KPI Dictionary đã được BA phê duyệt, mọi KPI được đánh dấu `BlockedByBusinessDefinition`; bản tổng hợp technical preview không được mô tả như số liệu nghiệp vụ đã chốt.

## 7. Danh mục truy vấn (Query Catalog)

Catalog allow-list version 1 có các ID cho revenue/GMV/orders/AOV/gross-profit/cancel-return/product-ranking/dangerous-inventory/summary. `orders.list.v1` đã được triển khai; các truy vấn KPI còn lại giữ trạng thái blocked/preview. EF Core parameterize bộ lọc; sắp xếp dùng allow-list trong code; mọi handler nhận `CancellationToken` và phạm vi bắt buộc.

## 8. Reporting API

- `GET /api/v1/reporting/kpis/summary`
- `GET /api/v1/reporting/revenue`
- `GET /api/v1/reporting/orders`
- `GET /api/v1/reporting/products/ranking`
- `GET /api/v1/reporting/inventory/dangerous`

API hỗ trợ khoảng thời gian/Branch và pagination/sort khi phù hợp; page size tối đa 200. Response có `data`, `meta`, `lastUpdatedAt`, cờ stale, query ID và Correlation ID. OpenAPI Integration Test xác minh tài liệu và cả năm contract endpoint đều thực thi.

## 9. Khả năng quan sát và audit

`X-Correlation-ID` hợp lệ được tái sử dụng; giá trị thiếu/không hợp lệ được tạo lại và trả trong response. JSON request log có path/status/latency/correlation/user/tenant/branch/query ID. Global error response dùng code/message/correlation; production không trả stack trace. `AuditLog` và `IAuditWriter` tồn tại; `orders.list.v1` ghi metadata của truy vấn nhạy cảm về quyền sau khi đã làm sạch.

## 10. Bảo mật và secret

Không có đầu vào raw SQL, ghi log raw JWT/password/API key hoặc bộ lọc Tenant do client cung cấp. `.env`, local settings, file key/certificate/secrets, output Build và thư mục làm việc được Git bỏ qua. Secret-pattern scan chỉ tìm thấy placeholder phát triển/credential demo rõ ràng. NuGet vulnerability audit cho biết không project nào có package chứa lỗ hổng theo feed hiện tại.

## 11. Health Check và khả năng phục hồi

`/health/live` không phụ thuộc DB; `/health/ready` gọi `CanConnectAsync`. SQL command timeout/request timeout là 10 giây. SQL transient retry tối đa 3 lần, không có retry vô hạn. Integration Test chạy SQLite quan hệ in-memory; Runtime chính vẫn là SQL Server.

## 12. Test đã chạy

- Unit: 11/11 PASS — metric/query catalog, từ chối raw query, kiểm tra date/filter/pagination, phạm vi role/Branch/Tenant.
- Integration: 17/17 PASS — auth/scope/isolation, cố gắng ghi đè Tenant, toàn bộ contract Reporting API, Health Check, Swagger, định dạng lỗi, tái sử dụng correlation, số lượng/ngày của Seed sáu tháng.
- Tổng: 28/28 PASS, 0 skipped.

## 13. Kết quả Build

`dotnet build Hosco.slnx --no-restore`: PASS, 0 errors. Trong sandbox, một lần Build có warning NU1900 do không truy cập được vulnerability feed; lệnh audit riêng có network đã chạy thành công và báo không có package chứa lỗ hổng.

## 14. KPI/quy tắc nghiệp vụ còn chờ BA chốt

1. Revenue: discount/trạng thái đơn hàng/ghi nhận refund.
2. GMV: các trạng thái đơn hàng được tính và cách xử lý gross/net discount.
3. Total Orders/AOV: trạng thái và mẫu số.
4. Gross Profit/Margin: phân bổ return/refund/discount; snapshot giá vốn đã sẵn sàng.
5. Cancellation/Return Rate: tính theo số lượng hay giá trị và mẫu số.
6. Top/Bottom SKU: theo revenue, quantity hay gross profit.
7. Dangerous Stock: `<= SafetyStock`, số ngày dự kiến, warehouse/Branch hoặc quy tắc khác.
8. Tổng hợp đa tiền tệ và múi giờ báo cáo.

## 15. Blocker

- Chưa có SRS v1/KPI Dictionary/Vai trò-Phân quyền/Luồng nghiệp vụ bản BA mới nhất để phê duyệt công thức KPI.
- SQL Server/LocalDB instance không khả dụng trên máy chạy hiện tại, nên database update là NOT RUN dù kiểm tra Migration/model/script là PASS.
- Git repository đã được khởi tạo local nhưng chưa commit/push. Người dùng sandbox cần dùng tùy chọn safe-directory theo từng lệnh để đọc trạng thái vì chủ sở hữu thư mục là người dùng Windows thật.

## 16. Mức sẵn sàng để chuyển sang GD3

GD3 có thể bắt đầu tích hợp Dashboard với Reporting API có version, dùng anomaly trong Seed và `lastUpdatedAt/isStale`, đồng thời xây dựng Alert Engine trên schema Alert/Audit. Trước khi nghiệm thu số KPI hoặc đặt ngưỡng cảnh báo, BA phải chốt các mục ở phần 14. GD4 đã có ranh giới API/Query Catalog; Chatbot không cần và không được cấp quyền truy cập trực tiếp cơ sở dữ liệu.

Ngoài phạm vi và chưa thực hiện: Dashboard UI, Alert Engine/scheduler hoàn chỉnh, Telegram/FCM, LLM/OpenAI/Gemini, chatbot UI/prompt/classifier và triển khai production.
