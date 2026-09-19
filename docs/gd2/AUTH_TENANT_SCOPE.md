# Xác thực và phạm vi Tenant/Branch

## Claims

JWT dùng trong môi trường phát triển chứa:

| Claim | Ý nghĩa |
|---|---|
| `sub` / NameIdentifier | ID người dùng bất biến |
| `role` | Một hoặc nhiều giá trị `Owner`, `BranchManager`, `ChainManager`, `SystemAdmin` |
| `tenant_id` | Tenant bất biến lấy từ danh tính đã lưu |
| `branch_id` | Không, một hoặc nhiều ID Branch được phân công |

`TenantId` được chủ động loại khỏi mọi contract bộ lọc báo cáo. Query parameter không xác định tên `tenantId` bị model binding bỏ qua và không thể tác động đến phạm vi được suy ra từ JWT claim.

## Quy tắc

| Chủ thể | Phạm vi hiệu lực |
|---|---|
| Owner | Chỉ Branch/Store được gán rõ ràng |
| Branch Manager | Chỉ các Branch được phân công; nếu bỏ `branchId` thì vẫn bị giới hạn |
| Chain Manager | Tất cả Branch đang hoạt động trong Tenant của mình |
| System Admin | Chỉ Branch báo cáo được gán rõ ràng; không tự động tenant-wide |

Khi có `branchId` cụ thể, `BranchScopeValidator` kiểm tra Branch thuộc Tenant trước, sau đó kiểm tra vai trò/phân công. Chỉ `ChainManager` có tenant-wide scope. Request tới Tenant khác hoặc Branch chưa được phân công trả về 403. Mỗi truy vấn báo cáo đều độc lập bổ sung điều kiện `TenantId == currentUser.TenantId` và điều kiện Branch đã được xác định. Trong MVP chưa có entity Store riêng: **Store scope = assigned Branch scope**.

## Minh chứng

Unit Test bao phủ policy về phạm vi. HTTP Integration Test bao phủ 401, Branch được phân công, Branch chưa được phân công, Branch khác Tenant, Chain Manager, cô lập Tenant khi không truyền Branch và hành vi cố gắng ghi đè bằng `tenantId`.
