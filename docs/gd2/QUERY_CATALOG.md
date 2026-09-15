# Danh mục truy vấn (Query Catalog)

Mọi mục đều là identifier có version và nằm trong allow-list. Bộ lọc được biểu diễn bằng biểu thức LINQ có kiểu; sắp xếp dùng allow-list phía server. Mọi implementation đều nhận `CancellationToken` và thực thi trong `ReportingScope` được suy ra từ server.

| QueryId | KPI | Đầu vào/bộ lọc | Đầu ra | Vai trò | Version | Trạng thái |
|---|---|---|---|---|---|---|
| `revenue.summary.v1` | KPI-01 | from, to, branchId | Tổng hợp doanh thu | Tất cả vai trò báo cáo | 1 | BlockedByBusinessDefinition |
| `revenue.trend.v1` | KPI-01 | from, to, branchId | `RevenuePoint[]` | Tất cả vai trò báo cáo | 1 | Technical preview; chờ quy tắc BA |
| `gmv.summary.v1` | KPI-02 | from, to, branchId | Giá trị KPI | Tất cả vai trò báo cáo | 1 | BlockedByBusinessDefinition |
| `orders.summary.v1` | KPI-03 | from, to, branchId | Giá trị KPI | Tất cả vai trò báo cáo | 1 | BlockedByBusinessDefinition |
| `orders.list.v1` | Không có | Khoảng thời gian, Branch, page/sort | `OrderRow` phân trang | Tất cả vai trò báo cáo | 1 | Implemented |
| `aov.summary.v1` | KPI-04 | from, to, branchId | Giá trị KPI | Tất cả vai trò báo cáo | 1 | BlockedByBusinessDefinition |
| `gross-profit.summary.v1` | KPI-05 | from, to, branchId | Giá trị KPI | Tất cả vai trò báo cáo | 1 | BlockedByBusinessDefinition |
| `cancel-return-rate.summary.v1` | KPI-06 | from, to, branchId | Giá trị KPI | Tất cả vai trò báo cáo | 1 | BlockedByBusinessDefinition |
| `products.ranking.v1` | KPI-07 | Khoảng thời gian, Branch, bottom, pageSize | `ProductRankRow[]` | Tất cả vai trò báo cáo | 1 | Technical preview; chờ chốt cơ sở xếp hạng |
| `inventory.dangerous.v1` | KPI-08 | Branch, pageSize | `DangerousInventoryRow[]` | Tất cả vai trò báo cáo | 1 | Technical preview; chờ quy tắc ngưỡng |
| `kpis.summary.v1` | KPI-01..08 | from, to, branchId | `KpiValue[]` | Tất cả vai trò báo cáo | 1 | ProvisionalTechnicalPreview |

Catalog trong code cung cấp mọi identifier nêu trên. Các mục bị chặn chỉ là contract; chúng không thể trở thành handler được phê duyệt cho đến khi BA xác nhận công thức và semantic version tương ứng.
