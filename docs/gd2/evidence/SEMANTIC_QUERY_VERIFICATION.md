# Xác minh Semantic Layer và Query Catalog (bằng chứng GD2 lịch sử)

> Snapshot này có trước Final GD1 và đã bị thay thế bởi catalog version 2.0 Implemented trong docs/gd2/QUERY_CATALOG.md.

## Metric catalog

Có tám định nghĩa: KPI-01 revenue, KPI-02 GMV, KPI-03 total orders, KPI-04 AOV, KPI-05 gross profit/margin, KPI-06 cancellation/return rate, KPI-07 top/bottom SKU và KPI-08 dangerous stock. Mỗi định nghĩa chứa code, name, description, unit, bộ lọc/chiều dữ liệu được hỗ trợ, version `1.0`, Query ID, trạng thái và blocker.

Cả tám đều có trạng thái `BlockedByBusinessDefinition`; lần audit này không đánh dấu công thức nghiệp vụ của chúng là đã xác minh.

## Query allow-list

| Query ID | Trạng thái trong catalog |
|---|---|
| `orders.list.v1` | Implemented |
| `kpis.summary.v1` | ProvisionalTechnicalPreview |
| `revenue.summary.v1` | BlockedByBusinessDefinition |
| `revenue.trend.v1` | BlockedByBusinessDefinition |
| `gmv.summary.v1` | BlockedByBusinessDefinition |
| `orders.summary.v1` | BlockedByBusinessDefinition |
| `aov.summary.v1` | BlockedByBusinessDefinition |
| `products.ranking.v1` | BlockedByBusinessDefinition |
| `inventory.dangerous.v1` | BlockedByBusinessDefinition |
| `gross-profit.summary.v1` | BlockedByBusinessDefinition |
| `cancel-return-rate.summary.v1` | BlockedByBusinessDefinition |

Catalog lưu vai trò được phép, parameter, kiểu đầu ra và version `1`. Query ID không xác định sẽ phát sinh exception thay vì chấp nhận SQL hoặc identifier tùy ý.

`ReportingFilter.Validate` buộc page >= 1, page size từ 1..200, `from <= to` và sort direction là `asc`/`desc`. Bộ lọc ngày/Branch được ghi trong các định nghĩa metric; mảng parameter riêng cho từng truy vấn tồn tại trong Query Catalog.

## Quy tắc đang chờ BA

Chưa được phê duyệt: trạng thái ghi nhận Revenue/cách xử lý refund và discount, trạng thái được tính vào GMV, trạng thái của Total Order, tử số/mẫu số AOV, phân bổ return cho Gross Profit, mẫu số Cancellation/Return, cơ sở xếp hạng và nguồn ngưỡng Dangerous Stock. Data store có phép tính preview nhưng đây không phải bằng chứng về công thức đã được phê duyệt. Response riêng của revenue/ranking/inventory không mang trạng thái catalog; xem defect về semantic contract trong `DEFECTS_FOUND.md`.

Kết luận: `PARTIALLY VERIFIED` (Xác minh một phần): catalog, allow-list, versioning và validation tồn tại, test PASS; semantic nghiệp vụ vẫn bị chặn bởi BA.
