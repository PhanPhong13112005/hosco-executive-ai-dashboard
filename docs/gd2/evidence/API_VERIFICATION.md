# Xác minh Reporting API v1

API được khởi chạy từ bản Build đã audit, kết nối SQL Server LocalDB, đồng thời bật rõ ràng Seed và Swagger. Process được dừng sau khi xác minh.

## Kiểm tra Runtime

| Method | Route | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|
| GET | `/health/live` | Process khỏe mạnh | 200; `Healthy`, không có dependency check | VERIFIED (Đã xác minh) |
| GET | `/health/ready` | Có thể kết nối cơ sở dữ liệu | 200; kiểm tra database `Healthy` | VERIFIED (Đã xác minh) |
| POST | `/api/v1/auth/login` | Danh tính demo hợp lệ trong cấu hình trả JWT | 200; token chỉ được giữ trong bộ nhớ và không ghi lại | VERIFIED (Đã xác minh) |
| GET | `/api/v1/reporting/orders?pageSize=1` | JWT Owner hợp lệ trả dữ liệu đúng phạm vi Tenant | 200; một dòng, tổng số 1,042 | VERIFIED (Đã xác minh) |
| GET | `/api/v1/reporting/orders?pageSize=1` | Request không có JWT bị từ chối | 401 | VERIFIED (Đã xác minh) |
| GET | `/api/v1/reporting/orders?branchId=<out-of-scope>&pageSize=1` | Branch Manager ngoài phạm vi phân công bị từ chối | 403 | VERIFIED (Đã xác minh) |
| GET | `/swagger/v1/swagger.json` | OpenAPI khả dụng khi được bật | 200; OpenAPI 3.0.4 | VERIFIED (Đã xác minh) |

Giá trị `X-Correlation-ID` được cung cấp đã được trả lại nguyên vẹn trong request có xác thực.

## Route trong source

| Method | Route | Handler |
|---|---|---|
| POST | `/api/v1/auth/login` | Đăng nhập |
| GET | `/api/v1/reporting/kpis/summary` | Bản xem trước KPI kỹ thuật |
| GET | `/api/v1/reporting/revenue` | Bản xem trước xu hướng Revenue |
| GET | `/api/v1/reporting/orders` | Danh sách đơn hàng phân trang |
| GET | `/api/v1/reporting/products/ranking` | Bản xem trước xếp hạng sản phẩm |
| GET | `/api/v1/reporting/inventory/dangerous` | Bản xem trước tồn kho nguy hiểm |

## OpenAPI

Artifact dạng text đã thu thập chứa sáu path (các controller route nêu trên), Bearer JWT security scheme và không phát hiện giá trị credential. Health Check endpoint được map trực tiếp nên không xuất hiện trong tài liệu OpenAPI sinh từ controller.

## Ngữ nghĩa Health Check

`/health/live` chủ động không chạy dependency check đã đăng ký. `/health/ready` chạy kiểm tra database có tag và trả về unhealthy khi `CanConnectAsync` thất bại. Trong lần audit này, cả hai đều healthy. Không tạo tình huống database không khả dụng trên LocalDB instance dùng chung. Khi cold start, hệ thống hiện apply Migration trước khi lắng nghe, vì vậy live endpoint có thể chưa khả dụng nếu SQL Server đã hỏng; nội dung này được ghi nhận là defect.

Kết luận: `VERIFIED` (Đã xác minh) cho các đường Runtime dùng SQL đã kiểm tra.
