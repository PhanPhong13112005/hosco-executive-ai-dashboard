# Xác minh dữ liệu mẫu (Seed Dataset)

## Tính xác định và khoảng thời gian

`DemoSeedIds.Id` tạo byte GUID từ SHA-256 của các nhãn ổn định. Generator dùng mốc UTC cố định và lặp 181 ngày, tạo timestamp đơn hàng từ 2026-01-01 08:00 UTC đến 2026-06-30 14:00 UTC. Vì vậy, ID, ngày, sản phẩm, người dùng và quy tắc anomaly có tính xác định khi chạy trên cơ sở dữ liệu trống.

Seed kết thúc ngay khi đã tồn tại bất kỳ Tenant nào. Điều này ngăn tạo dữ liệu trùng nhưng không sửa chữa cơ sở dữ liệu chỉ được Seed một phần.

## Số lượng trên SQL Server sau Migration và Seed

| Entity | Số lượng |
|---|---:|
| Tenants | 2 |
| Branches | 4 |
| Users | 5 |
| Roles | 4 |
| Customers | 32 |
| Employees | 12 |
| Products | 16 |
| Inventories | 32 |
| Orders | 2,084 |
| OrderItems | 4,168 |
| Payments | 2,084 |
| Refunds | 70 |
| Alerts | 4 |

Mỗi Tenant có 1,042 đơn hàng trên hai Branch. Một request xác minh API sau đó đã tạo một dòng audit; bản thân `DemoSeed` không chèn dòng audit.

## Anomaly fixture và dữ liệu cô lập

Có bốn Alert fixture cụ thể cho Tenant A và đã được truy vấn từ SQL Server: `abnormal-discount`, `cancellation-spike`, `low-stock` và `revenue-drop`. Generator cũng thay đổi tần suất đơn hàng, trạng thái hủy, mức giảm giá và số lượng tồn kho thấp trong các khoảng tương ứng.

Tenant B, hai Branch, Owner, Product, Customer, Employee, Inventory và bản ghi thương mại tương ứng tạo dữ liệu thực để kiểm tra cô lập khác Tenant.

Cả năm giá trị mật khẩu đã lưu đều khớp định dạng mã hóa PBKDF2-SHA256 dự kiến; không giá trị nào được in trong quá trình xác minh.

Kết luận: `VERIFIED` (Đã xác minh) cho cơ sở dữ liệu trống và implementation Seed đã audit; khả năng khôi phục database một phần là giới hạn đã biết.
