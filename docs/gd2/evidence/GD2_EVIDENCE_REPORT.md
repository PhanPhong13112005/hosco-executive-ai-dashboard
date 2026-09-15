# Báo cáo minh chứng GD2

## Repository

`D:\\Code\\hosco-executive-ai-dashboard`, branch `chore/gd2-evidence`.

## Commit được audit

`f75ac38c323ac2b61dd127081cc742852200f197`; tag `gd2-complete` trỏ đến cùng commit. Diff source so với commit này trống trước khi tạo evidence.

## Ngày thực hiện

2026-09-15 (Asia/Saigon).

## Môi trường

Windows 10.0.26200, .NET SDK 10.0.400, Runtime 10.0.11, EF CLI 10.0.11, SQL Server LocalDB 17.0.4025.3.

## GD2-01 Repository và cấu trúc project

Trạng thái: VERIFIED (Đã xác minh)

Minh chứng: `Hosco.slnx` chứa Domain, Application, Infrastructure, API, UnitTests và IntegrationTests. Project reference tuân thủ chiều phụ thuộc dự kiến. Cả sáu project đều Restore và Build thành công.

## GD2-02 ERD / Data Dictionary / Seed Dataset

Trạng thái: PARTIALLY VERIFIED (Xác minh một phần)

Minh chứng: Đã đếm 16 bảng ứng dụng từ model/Migration; SQL được sinh và Migration đã apply lên LocalDB. Truy vấn SQL xác nhận hai Tenant, bốn Branch, 2,084 Order trong khoảng 2026-01-01..2026-06-30, các bản ghi liên quan và bốn loại anomaly. Một số cột BranchId thiếu Branch FK vật lý.

## GD2-03 Auth / phạm vi Tenant/Branch

Trạng thái: VERIFIED (Đã xác minh)

Minh chứng: Đã xác minh JWT claim/validation, phạm vi Tenant suy ra từ claim, kiểm tra Branch, hash PBKDF2, các test và hành vi 401/403/200 trên Runtime dùng SQL.

## GD2-04 Semantic Layer / Query Catalog

Trạng thái: PARTIALLY VERIFIED (Xác minh một phần)

Minh chứng: Có tám contract KPI và 11 Query ID có version trong allow-list; test catalog/filter PASS. Chỉ danh sách đơn hàng có trạng thái đã triển khai, bản tổng hợp KPI là provisional và công thức BA vẫn bị chặn.

## GD2-05 Observability / audit

Trạng thái: PARTIALLY VERIFIED (Xác minh một phần)

Minh chứng: Đã xác minh việc tạo/tái sử dụng Correlation ID, metadata request có cấu trúc, ánh xạ exception, hành vi message lỗi an toàn cho production, schema/audit writer và việc dùng audit ở danh sách đơn hàng. Chưa có phạm vi audit đầy đủ cho mọi endpoint/nghiệp vụ.

## GD2-06 Secret / môi trường / Health Check

Trạng thái: PARTIALLY VERIFIED (Xác minh một phần)

Minh chứng: Có các ignore pattern và ranh giới cấu hình cần thiết; live và ready có kiểm tra database đều trả 200 trên LocalDB. Không xác định được production credential, nhưng tài liệu credential demo được track vẫn là một finding cần xem xét và liveness khi cold start còn phụ thuộc database.

## GD2-07 Reporting API v1

Trạng thái: VERIFIED (Đã xác minh)

Minh chứng: Tìm thấy năm reporting route và route login trong source/OpenAPI. Runtime dùng SQL đã xác minh login, báo cáo có xác thực, 401, 403, tái sử dụng correlation, Health Check và Swagger.

## Xác minh tự động

Build: PASS — 0 warnings, 0 errors.

Tests: 28/28 PASS — 11 Unit Test, 17 Integration Test, 0 failed, 0 skipped.

Artifact Migration: đã xác minh `20260914161316_InitialCreate`; model không có thay đổi chưa được Migration ghi nhận; đã sinh SQL idempotent.

DB Apply: PASS — Migration đã apply và dữ liệu đã Seed trên SQL Server LocalDB.

Dependency Audit: Truy vấn PASS; không có package chứa lỗ hổng theo feed hiện tại; các phiên bản outdated đã được ghi nhận và không update.

Secret Audit: PARTIAL — không xác định được production credential; tài liệu phát triển/demo và giới hạn của công cụ scan đã được ghi nhận.

## Hạng mục chờ BA

- Ghi nhận Revenue: trạng thái được tính, cách xử lý discount, thời điểm/phân bổ refund.
- GMV: trạng thái đơn hàng được tính/loại và cơ sở gross.
- Total Orders: trạng thái được tính và ngữ nghĩa thời gian.
- AOV: tử số, mẫu số, điều kiện loại trừ và hành vi khi không có đơn hàng.
- Gross Profit/Margin: cách dùng `UnitCostAtSale`, phân bổ refund/return và mẫu số margin.
- Cancellation/Return Rate: tính theo số lượng hay giá trị và mẫu số.
- Xếp hạng sản phẩm: theo revenue hay quantity, cách xử lý đồng hạng, quy tắc thời gian/trạng thái.
- Dangerous Stock: nguồn ngưỡng, phép so sánh và cấu hình ghi đè theo Branch/Product.

## Giới hạn đã biết

- Integration Test dùng SQLite; hành vi SQL Server được kiểm tra bằng Migration/Runtime riêng, không nằm trong bộ test tự động.
- Seed có tính xác định trên cơ sở dữ liệu trống nhưng không khôi phục dữ liệu bị thiếu một phần.
- AuditWriter chỉ được gọi cho báo cáo danh sách đơn hàng.
- Hành vi khi Health Check thất bại được kiểm tra từ code; dịch vụ LocalDB dùng chung không bị cố tình gián đoạn.
- OpenAPI không liệt kê các Health Check endpoint được map trực tiếp.
- Dependency scan và secret scan là kiểm tra cơ bản tại một thời điểm, không thay thế scanning trong CI.

## Kết luận

Nền tảng kỹ thuật GD2 có thể thực thi: mọi project đều Build, 28 test PASS, Migration apply thành công, dữ liệu xác định tồn tại trên SQL Server và API/OpenAPI có bảo mật hoạt động. Evidence không tuyên bố BA đã phê duyệt hoặc phạm vi audit đã đầy đủ. Các finding về toàn vẹn dữ liệu, Health Check khi cold start, nhãn Semantic, credential demo và cảnh báo truy vấn cần chủ sở hữu phân loại xử lý; không có business code nào bị thay đổi.
