# Từ điển dữ liệu kỹ thuật GD2

Tài liệu này đã được đồng bộ với Final GD1 Business Spec. KPI Dictionary là nguồn sự thật cho công thức KPI; phần dưới mô tả model kỹ thuật hỗ trợ các công thức đó. Trừ khi có ghi chú khác, mọi entity có `Id` đều có thêm `CreatedAt` và `UpdatedAt` không null (`datetimeoffset`) để phục vụ truy vết.

## Tenant

| Cột | Kiểu SQL | Null | Khóa | Mô tả/quy tắc |
|---|---|---:|---|---|
| Id | uniqueidentifier | Không | PK | Định danh Tenant |
| Code | nvarchar(50) | Không | UK | Mã Tenant ổn định |
| Name | nvarchar(200) | Không | | Tên hiển thị |
| IsActive | bit | Không | | Cờ bật/tắt Tenant |

## Branch

| Cột | Kiểu SQL | Null | Khóa | Mô tả/quy tắc |
|---|---|---:|---|---|
| Id | uniqueidentifier | Không | PK | Định danh Branch |
| TenantId | uniqueidentifier | Không | FK Tenant | Phân vùng bảo mật bắt buộc |
| Code | nvarchar(max) | Không | UK với TenantId | Mã Branch duy nhất trong Tenant |
| Name | nvarchar(max) | Không | | Tên hiển thị |
| Address | nvarchar(max) | Có | | Địa chỉ tùy chọn |
| IsActive | bit | Không | | Cờ bật/tắt Branch |

## AppUser

| Cột | Kiểu SQL | Null | Khóa | Mô tả/quy tắc |
|---|---|---:|---|---|
| Id | uniqueidentifier | Không | PK | Chủ thể đã xác thực |
| TenantId | uniqueidentifier | Không | FK Tenant | Nguồn của Tenant claim |
| Email | nvarchar(320) | Không | UK | Thông tin đăng nhập đã chuẩn hóa chữ thường |
| DisplayName | nvarchar(max) | Không | | Tên hiển thị không nhạy cảm |
| PasswordHash | nvarchar(500) | Không | | Hash mã hóa PBKDF2, không bao giờ lưu plaintext |
| IsActive | bit | Không | | Cờ cho phép đăng nhập |

## Role, UserRole, UserBranch

| Bảng.Cột | Kiểu SQL | Null | Khóa | Mô tả/quy tắc |
|---|---|---:|---|---|
| Role.Id | uniqueidentifier | Không | PK | ID vai trò |
| Role.Name | int | Không | UK | Enum `Owner`, `BranchManager`, `ChainManager`, `SystemAdmin` |
| UserRole.UserId | uniqueidentifier | Không | PK/FK AppUser | Người dùng được phân quyền |
| UserRole.RoleId | uniqueidentifier | Không | PK/FK Role | Vai trò được phân công |
| UserBranch.UserId | uniqueidentifier | Không | PK/FK AppUser | Người dùng bị giới hạn phạm vi |
| UserBranch.BranchId | uniqueidentifier | Không | PK/FK Branch | Branch được phép truy cập |

## Customer

| Cột | Kiểu SQL | Null | Khóa | Mô tả/quy tắc |
|---|---|---:|---|---|
| Id | uniqueidentifier | Không | PK | ID khách hàng |
| TenantId | uniqueidentifier | Không | FK Tenant | Phân vùng Tenant |
| Name | nvarchar(max) | Không | | Tên khách hàng; tránh ghi log |
| Email | nvarchar(max) | Có | Index với TenantId | PII tùy chọn; tránh ghi log |
| PhoneMasked | nvarchar(max) | Có | | Chỉ lưu giá trị đã che trong Seed demo |

## Employee

| Cột | Kiểu SQL | Null | Khóa | Mô tả/quy tắc |
|---|---|---:|---|---|
| Id | uniqueidentifier | Không | PK | ID nhân viên/thu ngân |
| TenantId | uniqueidentifier | Không | FK Tenant | Phân vùng Tenant |
| BranchId | uniqueidentifier | Không | FK Branch | Branch làm việc chính |
| EmployeeCode | nvarchar(max) | Không | UK với TenantId | Mã nhân viên ổn định |
| DisplayName | nvarchar(max) | Không | | Tên hiển thị |
| IsActive | bit | Không | | Cờ trạng thái làm việc/hệ thống |

## Product

| Cột | Kiểu SQL | Null | Khóa | Mô tả/quy tắc |
|---|---|---:|---|---|
| Id | uniqueidentifier | Không | PK | ID sản phẩm |
| TenantId | uniqueidentifier | Không | FK Tenant | Phân vùng Tenant |
| Sku | nvarchar(max) | Không | UK với TenantId | Đơn vị lưu kho |
| Name | nvarchar(max) | Không | | Tên sản phẩm |
| CurrentPrice | decimal(18,2) | Không | | Giá niêm yết hiện tại; không phải giá bán lịch sử |
| CurrentCost | decimal(18,2) | Không | | Giá vốn hiện tại; không được dùng tùy ý để tính GP lịch sử |
| FloorPrice | decimal(18,2) | Có | | Giá sàn SKU dùng cho AL-05; nullable/configurable |
| Currency | nvarchar(max) | Không | | Mã tiền tệ kiểu ISO; Seed dùng VND |
| IsActive | bit | Không | | Cờ hoạt động trong catalog |
| IsKeySku | bit | Không | | SKU quan trọng đủ điều kiện đánh giá AL-03 |

## Inventory

| Cột | Kiểu SQL | Null | Khóa | Mô tả/quy tắc |
|---|---|---:|---|---|
| Id | uniqueidentifier | Không | PK | ID dòng tồn kho |
| TenantId | uniqueidentifier | Không | FK Tenant | Phân vùng Tenant |
| BranchId | uniqueidentifier | Không | FK Branch, nhóm UK | Địa điểm lưu kho |
| ProductId | uniqueidentifier | Không | FK Product, nhóm UK | Sản phẩm được lưu kho |
| QuantityOnHand | int | Không | | Số lượng tồn hiện tại |
| ReservedQuantity | int | Không | | Số lượng đã giữ chỗ; `Available = QuantityOnHand - ReservedQuantity` |
| SafetyStock | int | Không | | Ngưỡng an toàn; nguy hiểm khi Available `<= SafetyStock` |

## Order

| Cột | Kiểu SQL | Null | Khóa | Mô tả/quy tắc |
|---|---|---:|---|---|
| Id | uniqueidentifier | Không | PK | ID đơn hàng |
| TenantId | uniqueidentifier | Không | FK Tenant | Điều kiện truy vấn bắt buộc |
| BranchId | uniqueidentifier | Không | FK Branch | Điều kiện Branch bắt buộc |
| CustomerId | uniqueidentifier | Có | FK Customer | Khách hàng tùy chọn |
| EmployeeId | uniqueidentifier | Không | FK Employee | Thu ngân xử lý |
| OrderNumber | nvarchar(max) | Không | UK với TenantId | Mã đơn hàng dễ đọc |
| Status | int | Không | | Pending=0, Completed=1, Cancelled=2, Returned=3, PartiallyReturned=4, Delivered=5; giá trị cũ không bị dịch chuyển |
| OrderedAt | datetimeoffset | Không | Có index | Thời điểm sự kiện nghiệp vụ |
| Subtotal | decimal(18,2) | Không | | Số tiền trước giảm giá cấp đơn hàng |
| DiscountAmount | decimal(18,2) | Không | | Số tiền giảm giá đơn hàng |
| TotalAmount | decimal(18,2) | Không | | Tổng tiền đã thu được lưu; cách diễn giải KPI chờ BA |
| Currency | nvarchar(max) | Không | | Mã tiền tệ |

## OrderItem

| Cột | Kiểu SQL | Null | Khóa | Mô tả/quy tắc |
|---|---|---:|---|---|
| Id | uniqueidentifier | Không | PK | ID dòng hàng |
| TenantId | uniqueidentifier | Không | FK Tenant | Phân vùng phòng vệ nhiều lớp |
| OrderId | uniqueidentifier | Không | FK Order | Đơn hàng cha |
| ProductId | uniqueidentifier | Không | FK Product | Sản phẩm đã bán |
| Quantity | int | Không | | Số lượng đã bán |
| UnitPrice | decimal(18,2) | Không | | Giá ghi nhận tại thời điểm giao dịch |
| UnitCostAtSale | decimal(18,2) | Không | | Snapshot giá vốn để tính COGS lịch sử |
| DiscountAmount | decimal(18,2) | Không | | Giảm giá dòng hàng |
| LineTotal | decimal(18,2) | Không | | Thành tiền dòng hàng đã lưu |

## Payment

| Cột | Kiểu SQL | Null | Khóa | Mô tả/quy tắc |
|---|---|---:|---|---|
| Id | uniqueidentifier | Không | PK | ID thanh toán |
| TenantId | uniqueidentifier | Không | FK Tenant | Phân vùng Tenant |
| OrderId | uniqueidentifier | Không | FK Order | Đơn hàng được thanh toán |
| Method | int | Không | | Cash/Card/BankTransfer/EWallet |
| Status | int | Không | | Pending/Paid/Failed/Refunded/PartiallyRefunded |
| Amount | decimal(18,2) | Không | | Số tiền thanh toán |
| PaidAt | datetimeoffset | Không | | Thời điểm thanh toán |

## Refund

| Cột | Kiểu SQL | Null | Khóa | Mô tả/quy tắc |
|---|---|---:|---|---|
| Id | uniqueidentifier | Không | PK | Bản ghi tài chính hoàn tiền/trả hàng |
| TenantId | uniqueidentifier | Không | FK Tenant | Phân vùng Tenant |
| BranchId | uniqueidentifier | Không | Có index | Phân vùng Branch |
| OrderId | uniqueidentifier | Không | FK Order | Đơn hàng gốc |
| Status | int | Không | | Requested/Approved/Rejected/Completed |
| Amount | decimal(18,2) | Không | | Số tiền hoàn |
| ReasonCode | nvarchar(max) | Không | | Mã lý do, không phải văn bản tự do |
| RequestedAt | datetimeoffset | Không | Có index | Thời điểm yêu cầu |
| CompletedAt | datetimeoffset | Có | | Thời điểm hoàn tất |

## RefundItem

| Cột | Kiểu SQL | Null | Khóa | Mô tả/quy tắc |
|---|---|---:|---|---|
| Id | uniqueidentifier | Không | PK | Phân bổ return/refund ở cấp dòng hàng |
| TenantId | uniqueidentifier | Không | FK Tenant | Phân vùng Tenant |
| RefundId | uniqueidentifier | Không | FK Refund | Refund cha |
| OrderItemId | uniqueidentifier | Không | FK OrderItem | Dòng bán được trả |
| Quantity | int | Không | | Số lượng trả; không được vượt số lượng bán |
| ReturnedValue | decimal(18,2) | Không | | Giá trị trả được khấu trừ Revenue; không giả lập partial return |

## Alert

| Cột | Kiểu SQL | Null | Khóa | Mô tả/quy tắc |
|---|---|---:|---|---|
| Id | uniqueidentifier | Không | PK | ID cảnh báo |
| TenantId | uniqueidentifier | Không | FK Tenant | Phân vùng Tenant |
| BranchId | uniqueidentifier | Có | FK logic Branch | Phạm vi Branch tùy chọn |
| Type | nvarchar(max) | Không | | Mã loại cảnh báo có thể version hóa |
| Severity | int | Không | | Medium=0, High=1, Critical=2; tương thích số với Info/Warning lịch sử |
| Status | int | Không | Có index | Open/Acknowledged/Resolved |
| Title | nvarchar(max) | Không | | Tiêu đề hiển thị |
| PayloadJson | nvarchar(max) | Không | | Metadata có thể mở rộng, không chứa secret |
| DetectedAt | datetimeoffset | Không | Có index | Thời điểm phát hiện/sự kiện |
| AcknowledgedAt | datetimeoffset | Có | | Thời điểm thao tác ở GD3 trong tương lai |
| ResolvedAt | datetimeoffset | Có | | Thời điểm thao tác ở GD3 trong tương lai |
| BaselineValue | decimal(18,2) | Có | | Baseline dùng làm bằng chứng tại thời điểm phát hiện |
| ResolutionNote | nvarchar(2000) | Có | | Ghi chú xử lý; bắt buộc với AL-04/AL-05 |
| EscalatedAt | datetimeoffset | Có | | Thời điểm đã phát escalation; ngăn gửi lặp cho cùng Alert |

## AuditLog

| Cột | Kiểu SQL | Null | Khóa | Mô tả/quy tắc |
|---|---|---:|---|---|
| Id | uniqueidentifier | Không | PK | ID sự kiện audit |
| TenantId | uniqueidentifier | Không | Có index | Phân vùng Tenant |
| UserId | uniqueidentifier | Có | FK logic AppUser | Danh tính thực hiện |
| BranchId | uniqueidentifier | Có | FK logic Branch | Branch hiệu lực/được yêu cầu |
| Action | nvarchar(max) | Không | | Ví dụ `reporting.query`, thao tác cảnh báo tương lai |
| ResourceType | nvarchar(max) | Không | | Loại tài nguyên được audit |
| ResourceId | nvarchar(max) | Có | | Định danh tài nguyên tùy chọn |
| QueryId | nvarchar(max) | Có | | Định danh truy vấn trong allow-list |
| CorrelationId | nvarchar(max) | Không | | Liên kết truy vết request |
| MetadataJson | nvarchar(max) | Không | | Metadata có cấu trúc đã làm sạch |
| OccurredAt | datetimeoffset | Không | Có index | Thời điểm sự kiện audit |
