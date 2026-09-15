# Reporting API v1

Base path: `/api/v1`. Các reporting endpoint yêu cầu Bearer JWT và policy `ReportingReader`.

| Method/path | Query ID | Đầu vào | Kết quả |
|---|---|---|---|
| `POST /auth/login` | n/a | email, password | JWT và thời điểm hết hạn |
| `GET /reporting/kpis/summary` | `kpis.summary.v1` | Bộ lọc ngày/Branch | Tất cả contract KPI kèm trạng thái định nghĩa |
| `GET /reporting/revenue` | `revenue.trend.v1` | Bộ lọc ngày/Branch | Số tiền theo tiền tệ từng ngày (preview) |
| `GET /reporting/orders` | `orders.list.v1` | Ngày/Branch, page, pageSize, sort | Danh sách đơn hàng phân trang |
| `GET /reporting/products/ranking` | `products.ranking.v1` | Ngày/Branch, `bottom`, pageSize | Danh sách sản phẩm đã xếp hạng (preview) |
| `GET /reporting/inventory/dangerous` | `inventory.dangerous.v1` | Branch, pageSize | Tồn kho thấp hơn/bằng mức an toàn (preview) |
| `GET /health/live` | n/a | Không có | Trạng thái tiến trình |
| `GET /health/ready` | n/a | Không có | Mức sẵn sàng của cơ sở dữ liệu |

Bộ lọc chung: `from`, `to`, `branchId`, `page` (mặc định 1), `pageSize` (mặc định 50, tối đa 200), `sortBy`, `sortDirection`. Các cách sắp xếp đơn hàng được hỗ trợ là `orderedAt`, `orderNumber` và `amount`; giá trị không xác định sẽ quay về mới nhất trước. `TenantId` không phải đầu vào.

Response báo cáo thành công có dạng `{ data, meta }`; metadata gồm khoảng ngày hiệu lực, Branch được yêu cầu, `lastUpdatedAt`, `isStale`, `queryId`, `correlationId` và thông tin phân trang khi phù hợp. Error middleware trả 400/403/404/409/500 với code/message/Correlation ID ổn định. Authentication middleware trả 401 khi JWT thiếu hoặc không hợp lệ.

Swagger/OpenAPI mô tả model request/response và xác thực Bearer tại `/swagger` trong môi trường Development. Dashboard và Chatbot tương lai phải sử dụng các contract này; cả hai đều không được thực thi SQL tùy ý.
