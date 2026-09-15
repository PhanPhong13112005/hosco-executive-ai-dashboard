# Xác minh cơ sở dữ liệu

## Model và Migration

Kiểm tra source cho thấy chính xác 16 `DbSet`/bảng ứng dụng:

`Tenant`, `Branch`, `AppUser` (bảng `Users`), `Role`, `UserRole`, `UserBranch`, `Customer`, `Employee`, `Product`, `Inventory`, `Order`, `OrderItem`, `Payment`, `Refund`, `Alert` và `AuditLog`.

SQL idempotent đã sinh chứa 17 câu lệnh `CREATE TABLE`: 16 bảng ứng dụng cộng với bảng `__EFMigrationsHistory` của EF Core. File chứa 26 khai báo khóa ngoại.

| Nội dung kiểm tra | Kết quả thực tế |
|---|---|
| EF CLI | Entity Framework Core tools 10.0.11 |
| Artifact Migration | Tìm thấy `20260914161316_InitialCreate` |
| Model drift | `No changes have been made to the model since the last migration.` |
| Sinh SQL | PASS; đã sinh `sql/gd2-schema.sql` theo dạng idempotent |
| SQL Server | LocalDB `MSSQLLocalDB` 17.0.4025.3 đang chạy |
| Database apply | PASS; Migration đầu tiên đã apply lên `HoscoDev` |
| Danh sách sau khi apply | Migration được liệt kê mà không có `(Pending)` |

## Minh chứng mapping

- Mọi entity thuộc Tenant đều kế thừa `TenantEntity` và nhận khóa ngoại hạn chế xóa `TenantId -> Tenants.Id`.
- Khóa ngoại Branch vật lý tồn tại cho `Employee`, `Inventory`, `Order` và `UserBranch`.
- `Order` có quan hệ vật lý với Branch, Customer tùy chọn và Employee. `OrderItem` có quan hệ với Order và Product; `Payment` và `Refund` liên kết với Order.
- `OrderItem.UnitCostAtSale` tồn tại và được map thành `decimal(18,2)` để lưu snapshot giá vốn lịch sử.
- Các trường tiền tệ dùng precision 18,2.
- Các index quan trọng đã xác minh gồm Tenant/Code, Tenant/SKU, Tenant/EmployeeCode, Tenant/Branch/Product của Inventory, Tenant/OrderNumber, Tenant/Branch/OrderDate, Tenant/OrderItem, Tenant/Branch/RefundDate, Tenant/Status/AlertDate và Tenant/AuditDate.

## Giới hạn về toàn vẹn dữ liệu

`Refund.BranchId`, `Alert.BranchId` và `AuditLog.BranchId` là cột/đầu vào index nhưng không có khóa ngoại vật lý tới Branch. Các khóa ngoại Tenant và Branch tách biệt trên những bảng khác cũng không tạo thành ràng buộc tổng hợp để chứng minh cả hai ID thuộc cùng một Tenant. Nội dung này được ghi nhận trong `DEFECTS_FOUND.md`; model và Migration không bị thay đổi trong quá trình audit.

Kết luận: `PARTIALLY VERIFIED` (Xác minh một phần) vì artifact, quá trình sinh và apply đã được xác minh, trong khi toàn vẹn tham chiếu Branch chưa đầy đủ.
