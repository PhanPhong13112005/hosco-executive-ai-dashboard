# Chỉ mục minh chứng GD2

- Repository được audit: `D:\\Code\\hosco-executive-ai-dashboard`
- Branch: `chore/gd2-evidence`
- Checkpoint: `f75ac38c323ac2b61dd127081cc742852200f197` (`gd2-complete`)
- Ngày audit: 2026-09-15 (Asia/Saigon)

Các trạng thái trong chỉ mục này được giới hạn ở `VERIFIED` (Đã xác minh), `PARTIALLY VERIFIED` (Xác minh một phần), `NOT VERIFIED` (Chưa xác minh) và `NOT RUN` (Chưa chạy).

| Hạng mục GD2 | Minh chứng trong source | Minh chứng Runtime | Trạng thái |
|---|---|---|---|
| GD2-01 Repository và cấu trúc project | `Hosco.slnx`, sáu file project, project reference, `src/`, `tests/`, `docs/gd2/` | Restore/Build hoàn tất cho cả sáu project; đã kiểm tra Git branch/tag/commit | VERIFIED (Đã xác minh) |
| GD2-02 ERD / Data Dictionary / Seed Dataset | `ERD.md`, `DATA_DICTIONARY.md`, 16 `DbSet`, Migration đầu tiên, `DemoSeed` xác định | Migration đã apply lên SQL Server LocalDB; đã sinh artifact SQL; đã truy vấn số lượng dữ liệu Seed | PARTIALLY VERIFIED (Xác minh một phần) |
| GD2-03 Auth / phạm vi Tenant/Branch | Cấu hình JWT, `CurrentUser`, `IdentityStore`, `BranchScopeValidator`, `ReportingScopeFactory` | Test 401, 403, request đã xác thực, cô lập khác Tenant và chống ghi đè bằng query parameter đều PASS | VERIFIED (Đã xác minh) |
| GD2-04 Semantic Layer / Query Catalog | Tám định nghĩa metric, 11 Query ID trong allow-list, metadata version và bộ lọc | Test catalog/filter PASS; chỉ `orders.list.v1` đã triển khai và công thức KPI vẫn chờ BA | PARTIALLY VERIFIED (Xác minh một phần) |
| GD2-05 Observability / audit baseline | Correlation/exception middleware, JSON logging, `AuditLog`, `AuditWriter` | Test tái sử dụng Correlation ID PASS; request báo cáo Runtime đã tạo một dòng audit | PARTIALLY VERIFIED (Xác minh một phần) |
| GD2-06 Secret / môi trường / Health Check | `.gitignore`, `.env.example`, file settings, đăng ký live/ready | live và ready dùng SQL đều trả 200; không xác định được production credential, nhưng tài liệu credential demo được track cần xem xét | PARTIALLY VERIFIED (Xác minh một phần) |
| GD2-07 Reporting API v1 | `AuthController`, `ReportingController`, `ReportingDataStore`, cấu hình OpenAPI | API dùng SQL: login 200, báo cáo 200, chưa xác thực 401, ngoài phạm vi 403; đã tải OpenAPI | VERIFIED (Đã xác minh) |

## Cấu trúc project và quan hệ tham chiếu

| Layer/project | Vai trò đã xác minh | Project reference trực tiếp |
|---|---|---|
| `Hosco.Domain` | Entity, kiểu cơ sở dùng chung, enum | Không có |
| `Hosco.Application` | Interface, model báo cáo, dịch vụ phạm vi, Semantic/Query Catalog | `Hosco.Domain` |
| `Hosco.Infrastructure` | EF Core mapping/Migration/Seed, reporting/identity store, xác minh mật khẩu, audit writer | `Hosco.Domain`, `Hosco.Application` |
| `Hosco.Api` | HTTP controller, JWT/authorization, middleware, Health Check, composition root, Swagger | `Hosco.Application`, `Hosco.Infrastructure` |
| `Hosco.UnitTests` | Unit Test cho catalog, bộ lọc và phạm vi Branch | `Hosco.Application`, `Hosco.Domain` |
| `Hosco.IntegrationTests` | HTTP API bằng child process thực với cơ sở dữ liệu quan hệ SQLite in-memory | `Hosco.Api` (`ReferenceOutputAssembly=false`, `Private=false`) |

## Minh chứng chi tiết

- [Xác minh Build](BUILD_VERIFICATION.md)
- [Xác minh Test](TEST_VERIFICATION.md)
- [Xác minh cơ sở dữ liệu](DATABASE_VERIFICATION.md)
- [Xác minh Seed](SEED_VERIFICATION.md)
- [Xác minh bảo mật](SECURITY_VERIFICATION.md)
- [Xác minh Semantic Layer và Query Catalog](SEMANTIC_QUERY_VERIFICATION.md)
- [Xác minh API](API_VERIFICATION.md)
- [Xác minh Observability và audit](OBSERVABILITY_AUDIT_VERIFICATION.md)
- [Audit secret](SECRET_AUDIT.md)
- [Audit dependency](DEPENDENCY_AUDIT.md)
- [Các defect đã phát hiện](DEFECTS_FOUND.md)
- [Báo cáo tổng hợp](GD2_EVIDENCE_REPORT.md)
- [SQL đã sinh](sql/gd2-schema.sql)
- [OpenAPI đã thu thập](openapi/gd2-openapi.json)
