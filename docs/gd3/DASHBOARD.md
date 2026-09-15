# Executive Dashboard GD3

## Màn hình

Dashboard Web có sidebar, bộ lọc ngày/Branch, tám KPI card, Revenue Trend, Alert highlight, Top/Bottom SKU và bảng tồn kho nguy hiểm. KPI card mở drill-down theo thời gian. UI hỗ trợ responsive và bốn state bắt buộc: loading, empty, error, permission denied.

## Nguồn dữ liệu

UI chỉ gọi Reporting API v1 qua `src/Hosco.Web/src/api/client.ts`. Không có truy vấn database trực tiếp.

| Thành phần | Endpoint |
|---|---|
| KPI cards | `GET /api/v1/reporting/dashboard/summary` |
| Revenue Trend | `GET /api/v1/reporting/revenue/trend` |
| Order Trend | `GET /api/v1/reporting/orders/trend` |
| Top SKU | `GET /api/v1/reporting/products/top` |
| Bottom SKU | `GET /api/v1/reporting/products/bottom` |
| Dangerous Stock | `GET /api/v1/reporting/inventory/dangerous` |
| Drill-down | `GET /api/v1/reporting/kpis/{metricId}/drilldown` |
| Branch filter options | `GET /api/v1/reporting/branches` |

Mọi endpoint hỗ trợ filter phù hợp `from`, `to`, `branchId` và dùng scope từ JWT. Các endpoint GD2 cũ vẫn được giữ nguyên.

## Trạng thái công thức

Summary hiện là `ProvisionalTechnicalPreview`. Phép tổng hợp dùng field kỹ thuật hiện có để demo, nhưng không được xem là công thức nghiệp vụ đã phê duyệt. Response chứa `definitionStatus` và `note`; UI luôn hiển thị cảnh báo PENDING BA.

- Revenue preview: tổng `TotalAmount` của đơn `Completed`.
- AOV preview: trung bình `TotalAmount` của đơn `Completed`.
- Gross Profit preview: `LineTotal - UnitCostAtSale * Quantity` của dòng thuộc đơn `Completed`; chưa phân bổ return/refund.
- Cancellation/Return preview: tỷ lệ số đơn theo status trên tổng đơn trong filter.
- SKU ranking preview: xếp theo `LineTotal`.
- Dangerous Stock preview: `QuantityOnHand <= SafetyStock` hoặc ngưỡng rule cấu hình.

