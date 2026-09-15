# Xác minh Test

## Kết quả thực thi

`dotnet test` và `dotnet test Hosco.slnx --no-build --logger "trx;LogFileName=gd2-tests.trx"` đều hoàn tất thành công.

| Bộ test | Passed | Failed | Skipped | Tổng | Thời gian do test runner báo cáo |
|---|---:|---:|---:|---:|---:|
| `Hosco.UnitTests` | 11 | 0 | 0 | 11 | 58 ms trong lần chạy TRX |
| `Hosco.IntegrationTests` | 17 | 0 | 0 | 17 | 1 giây trong lần chạy TRX |
| Tổng | 28 | 0 | 0 | 28 | Các giá trị theo từng bộ test như trên |

Các file TRX được tạo trong thư mục `TestResults/` đã bị ignore của từng project test và chủ động không được đưa vào artifact minh chứng.

## Đối chiếu phạm vi kiểm thử với yêu cầu

| Phạm vi | Test method/case | Minh chứng |
|---|---|---|
| Cô lập Tenant | `Cross_tenant_branch_is_always_forbidden`; `Cross_tenant_branch_is_forbidden`; `Reporting_never_leaks_other_tenant_data`; `Client_tenantId_query_parameter_cannot_override_claim` | Unit Test và HTTP pipeline thực |
| Phạm vi Branch | `Branch_manager_can_access_assigned_branch`; `Branch_manager_cannot_access_unassigned_branch`; `Chain_manager_can_access_any_branch_in_same_tenant`; các case HTTP tương ứng cho quyền được phân công, chưa phân công và Chain Manager | Unit Test và Integration Test |
| Auth | `Reporting_without_login_returns_401`; mọi test endpoint có xác thực đều lấy JWT qua `/api/v1/auth/login` | Integration Test |
| Reporting API | `Reporting_contract_endpoints_execute` có bốn route case; các test lấy đơn hàng; `Invalid_filter_returns_400`; `OpenApi_document_is_available_when_enabled` | Integration Test |
| Seed Dataset | `Seed_exposes_six_month_deterministic_tenant_dataset` xác minh 1,042 đơn hàng Tenant A và timestamp đầu tiên 2026-01-01 08:00 UTC | Integration Test |
| Semantic/Query Catalog | `Metric_catalog_contains_all_eight_kpis_and_marks_unapproved_definitions`; `Query_catalog_rejects_non_allowlisted_query` | Unit Test |
| Kiểm tra bộ lọc | `Pagination_rejects_out_of_range_values` có ba case; `Filter_rejects_inverted_date_range`; `Filter_accepts_maximum_page_size` | Unit Test |
| Health Check | `Health_endpoint_is_healthy` bao phủ `/health/live` và `/health/ready` | Integration Test |
| Correlation | `Valid_correlation_id_is_reused`; test 401 cũng xác nhận response header tồn tại | Integration Test |

Giới hạn: các test không thực thi quá trình dịch truy vấn riêng của SQL Server; API fixture dùng SQLite quan hệ in-memory. Vì vậy, SQL Server được xác minh riêng trong kiểm tra Runtime và cơ sở dữ liệu.

Kết luận: `VERIFIED` (Đã xác minh) cho bộ test đã chạy; các giới hạn bao phủ vẫn được ghi nhận.
