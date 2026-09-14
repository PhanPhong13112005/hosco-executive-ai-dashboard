# GD2 Completion Report

## 1. Tổng quan

Đã hoàn thành backend foundation .NET 10 cho HOSCO theo kiến trúc Domain/Application/Infrastructure/API. Reporting API là boundary duy nhất cho Dashboard và Future Chatbot; không có đường Chatbot → Database hay arbitrary SQL.

## 2. File đã tạo/sửa

- Solution/project: `Hosco.slnx`, `global.json`, 4 source projects, 2 test projects, local `dotnet-ef` manifest.
- Domain: organization/auth, commerce/inventory/order/payment/refund, alert/audit entities và enums.
- Application: current-user/scope contracts, filter/response contracts, metric/query catalogs, branch/reporting scope rules.
- Infrastructure: SQL Server `DbContext`, mappings/index/FK, migration, deterministic seed, identity/password, reporting/audit stores.
- API: JWT/auth controller, five Reporting API endpoints, correlation/exception/request logging, health, timeout, Swagger.
- Operations/docs: `.gitignore`, `.env.example`, appsettings, request examples, README và toàn bộ `docs/gd2`.

## 3. Database/ERD

Schema có 16 table chính: Tenant, Branch, AppUser, Role, UserRole, UserBranch, Customer, Employee, Product, Inventory, Order, OrderItem, Payment, Refund, Alert, AuditLog. Tenant FK và các reporting index được tạo. `OrderItem.UnitCostAtSale` lưu cost snapshot lịch sử. Mermaid ERD nằm trong `ERD.md`; technical fields nằm trong `DATA_DICTIONARY.md`.

Migration `20260914161316_InitialCreate` đã được sinh. `dotnet-ef migrations has-pending-model-changes` trả về “No changes”; idempotent SQL Server script sinh thành công (19,503 byte). Apply lên SQL Server/LocalDB không chạy vì máy hiện tại không có LocalDB instance khả dụng; command chuẩn đã có trong README.

## 4. Seed Dataset

Generator dùng SHA-256-derived deterministic GUIDs và mốc cố định 2026-01-01 đến 2026-06-30. Dataset gồm 2 tenant, 4 branch, 5 demo identities, 8 product/tenant, customer, cashier, inventory, 2,084 order, 4,168 order item, 2,084 payment và refund fixtures. Alert fixture đánh dấu cancellation spike, revenue drop, low stock và abnormal discount. Seed không lưu plaintext password; demo password được PBKDF2-SHA256 hash.

## 5. Auth/Tenant/Branch

JWT xác minh issuer/audience/signature/lifetime và chứa `sub`, role, `tenant_id`, `branch_id`. `CurrentUser` là nơi duy nhất parse claims. `ReportingScopeFactory` lấy tenant từ identity, xác minh branch thuộc tenant, áp scope assigned cho Branch Manager kể cả khi bỏ `branchId`, và giữ System Admin tenant-scoped.

Integration test xác nhận unauthenticated 401, branch hợp lệ, branch ngoài quyền 403, foreign tenant 403, Chain Manager own-tenant access, không leak tenant khi bỏ branch, và query parameter `tenantId` không override claim.

## 6. Semantic Layer

Metric catalog có KPI-01 đến KPI-08 với code/name/description/unit/filters/dimensions/version/query ID/status/blocker. Vì không có KPI Dictionary BA đã duyệt, mọi KPI được đánh dấu `BlockedByBusinessDefinition`; summary technical preview không được mô tả như số liệu business đã chốt.

## 7. Query Catalog

Catalog allow-list version 1 có revenue/GMV/orders/AOV/gross-profit/cancel-return/product-ranking/dangerous-inventory/summary IDs. `orders.list.v1` implemented; các KPI query còn lại giữ blocked/preview status. EF Core parameterize filter; sort dùng code allow-list; tất cả handler nhận `CancellationToken` và scope bắt buộc.

## 8. Reporting API

- `GET /api/v1/reporting/kpis/summary`
- `GET /api/v1/reporting/revenue`
- `GET /api/v1/reporting/orders`
- `GET /api/v1/reporting/products/ranking`
- `GET /api/v1/reporting/inventory/dangerous`

API hỗ trợ range/branch và pagination/sort khi phù hợp; page size tối đa 200. Response có `data`, `meta`, `lastUpdatedAt`, stale flag, query ID và correlation ID. OpenAPI integration test xác minh document và cả năm contract endpoint đều thực thi.

## 9. Observability/Audit

`X-Correlation-ID` hợp lệ được reuse, giá trị thiếu/không hợp lệ được tạo lại và trả trong response. JSON request log có path/status/latency/correlation/user/tenant/branch/query ID. Global error response dùng code/message/correlation; production không trả stack trace. `AuditLog` và `IAuditWriter` tồn tại; `orders.list.v1` ghi permission-sensitive query metadata đã sanitize.

## 10. Security/Secrets

Không có raw SQL input, raw JWT/password/API key logging hoặc client tenant filter. `.env`, local settings, key/certificate/secrets files, build và work outputs được gitignore. Secret-pattern scan chỉ tìm thấy explicit development placeholders/demo credential. NuGet vulnerability audit báo không project nào có vulnerable package theo feed hiện tại.

## 11. Health/Resilience

`/health/live` không phụ thuộc DB; `/health/ready` gọi `CanConnectAsync`. SQL command/request timeout là 10 giây. SQL transient retry tối đa 3 lần. Không có infinite retry. Integration tests chạy relational SQLite in-memory; runtime chính vẫn là SQL Server.

## 12. Test đã chạy

- Unit: 11/11 PASS — metric/query catalog, raw query rejection, date/filter/pagination validation, role/branch/tenant scope.
- Integration: 17/17 PASS — auth/scope/isolation, attempted tenant override, all Reporting API contracts, health, Swagger, error format, correlation reuse, six-month seed count/date.
- Tổng: 28/28 PASS, 0 skipped.

## 13. Kết quả build

`dotnet build Hosco.slnx --no-restore`: PASS, 0 errors. Trong sandbox, một lần build có warning NU1900 do vulnerability feed không truy cập được; audit command riêng có network đã chạy thành công và báo không có vulnerable package.

## 14. KPI/business rule còn chờ BA chốt

1. Revenue: discount/order status/refund recognition.
2. GMV: included order statuses và gross/net discount treatment.
3. Total Orders/AOV: status và denominator.
4. Gross Profit/Margin: return/refund/discount allocation; cost snapshot đã sẵn sàng.
5. Cancellation/Return Rate: count hay amount/value và mẫu số.
6. Top/Bottom SKU: revenue, quantity hay gross profit.
7. Dangerous Stock: `<= SafetyStock`, projected days, warehouse/branch hoặc rule khác.
8. Currency/multi-currency aggregation và reporting timezone.

## 15. Blocker

- Chưa có SRS v1/KPI Dictionary/Vai trò-Phân quyền/Luồng nghiệp vụ bản BA mới nhất để sign off KPI formulas.
- SQL Server/LocalDB instance không khả dụng trên máy chạy hiện tại, nên database update là NOT RUN dù migration/model/script validation PASS.
- Git repository đã được khởi tạo local nhưng không commit/push. Sandbox user cần dùng per-command safe-directory override để đọc status vì owner của thư mục là Windows user thật.

## 16. Việc sẵn sàng chuyển sang GD3

GD3 có thể bắt đầu tích hợp Dashboard vào versioned Reporting API, dùng seed anomaly và `lastUpdatedAt/isStale`, đồng thời xây Alert Engine trên Alert/Audit schema. Trước khi nghiệm thu số KPI hoặc đặt alert threshold, BA phải chốt các mục ở phần 14. GD4 có sẵn boundary API/query catalog; không cần và không được cấp direct DB access cho Chatbot.

Ngoài phạm vi và chưa làm: Dashboard UI, Alert Engine/scheduler hoàn chỉnh, Telegram/FCM, LLM/OpenAI/Gemini, chatbot UI/prompt/classifier và production deployment.
