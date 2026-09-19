# Danh mục truy vấn (Query Catalog)

Mọi mục đều là identifier có version và nằm trong allow-list. Bộ lọc được biểu diễn bằng biểu thức LINQ có kiểu; sắp xếp dùng allow-list phía server. Mọi implementation đều nhận `CancellationToken` và thực thi trong `ReportingScope` được suy ra từ server.

| QueryId | KPI | Đầu vào/bộ lọc | Đầu ra | Vai trò | Version | Trạng thái |
|---|---|---|---|---|---|---|
| `revenue.summary.v1` | KPI-01 | from, to, branchId | Net Revenue sau discount/return | Tất cả vai trò báo cáo | 2 | Implemented |
| `revenue.trend.v1` | KPI-01 | from, to, branchId | `RevenuePoint[]`, nhóm ngày UTC+7 | Tất cả vai trò báo cáo | 2 | Implemented |
| `gmv.summary.v1` | KPI-02 | from, to, branchId | Giá trước discount theo valid quantity | Tất cả vai trò báo cáo | 2 | Implemented |
| `orders.summary.v1` | KPI-03 | from, to, branchId | Distinct recognized sale orders | Tất cả vai trò báo cáo | 2 | Implemented |
| `orders.list.v1` | Không có | Khoảng thời gian, Branch, page/sort | `OrderRow` phân trang | Tất cả vai trò báo cáo | 1 | Implemented |
| `aov.summary.v1` | KPI-04 | from, to, branchId | Revenue / Total Orders, nullable | Tất cả vai trò báo cáo | 2 | Implemented |
| `gross-profit.summary.v1` | KPI-05 | from, to, branchId | GP/GM theo valid quantity | Tất cả vai trò báo cáo | 2 | Implemented |
| `cancel-return-rate.summary.v1` | KPI-06 | from, to, branchId | Distinct affected / all created | Tất cả vai trò báo cáo | 2 | Implemented |
| `products.ranking.v1` | KPI-07 | Khoảng thời gian, Branch, bottom, pageSize | Xếp theo valid quantity, tie SKU/ProductId | Tất cả vai trò báo cáo | 2 | Implemented |
| `inventory.dangerous.v1` | KPI-08 | Branch, pageSize | Available `<=` SafetyStock | Tất cả vai trò báo cáo | 2 | Implemented |
| `kpis.summary.v1` | KPI-01..08 | from, to, branchId | `KpiValue[]` | Tất cả vai trò báo cáo | 2 | Implemented |

Catalog trong code cung cấp mọi identifier nêu trên. Công thức dùng chung `KpiCalculator`/`KpiSnapshotStore`; Alert Engine và Reporting không định nghĩa lại công thức KPI. Các số ngưỡng Alert vẫn là BA đề xuất/configurable, không làm thay đổi trạng thái đã-final của KPI Dictionary.
