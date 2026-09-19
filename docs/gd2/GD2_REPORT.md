# Báo cáo GD2 sau đồng bộ Final GD1

## Kết quả

Reporting backend .NET 10 đã được đồng bộ với Final GD1 Business Spec. `SemanticCatalog` và `QueryCatalog` đánh dấu KPI-01..08 `Implemented` phiên bản 2.0. Dashboard, drill-down, ranking và Alert signals dùng chung `KpiCalculator`/`KpiSnapshotStore`; không có đường Web/Chatbot truy cập DB trực tiếp.

## KPI canonical

- Revenue: recognized sales trừ discount và item-level returned value, VND nguyên.
- GMV: `UnitPrice × valid quantity`, trước discount.
- Total Orders: distinct recognized Completed/Delivered lifecycle orders. `Returned`/`PartiallyReturned` được diễn giải là post-completion sale và vẫn là recognized order sau khi khấu trừ return.
- AOV: Revenue / Total Orders; null khi không có đơn.
- COGS/GP/GM: `UnitCostAtSale × valid quantity`; margin null khi Revenue=0.
- Cancellation/Return Rate: distinct affected orders / all created orders.
- SKU ranking: valid quantity, zero bị loại, tie theo SKU rồi ProductId.
- Dangerous Stock: `OnHand - Reserved <= SafetyStock`.

Mọi filter ngày/grouping nghiệp vụ dùng UTC+7. Model bổ sung `Delivered=5` mà không đổi giá trị enum cũ, `RefundItem`, `ReservedQuantity`, `IsKeySku`, `FloorPrice`.

## Security/scope

Tenant luôn lấy từ JWT. Owner, BranchManager và SystemAdmin chỉ có Branch được gán rõ ràng; ChainManager tenant-wide trong tenant đã cấp. Store scope được ánh xạ sang assigned Branch scope trong MVP. Cross-tenant và unassigned-branch bị từ chối.

## Migration/seed

Migration mới duy nhất: `SyncFinalGd1BusinessRules`; không sửa `InitialCreate` hoặc migration GD3 cũ. Seed deterministic có Completed/Delivered/Cancelled/full return/partial return, RefundItem, Reserved/key SKU/FloorPrice, explicit user scope và fixtures AL-01..05 cho hai tenant.

## Validation hiện tại

- Build: PASS, 0 errors; NU1900 là cảnh báo feed môi trường.
- Unit Test: 53/53 PASS.
- Integration Test: 36/36 PASS.
- Frontend build: PASS, 24 modules.

SQL Server/LocalDB migration/runtime phải được ghi theo lần chạy thực tế; known intermittent local WDAC được mô tả riêng tại `docs/gd3/KNOWN_ENVIRONMENT_ISSUES.md`.
