# Báo cáo GD3 sau đồng bộ Final GD1

GD3 hiện dùng KPI Dictionary làm business truth, UTC+7 tập trung, explicit RBAC scope và Alert Catalog AL-01..05. Dashboard/API/UI không còn technical-preview KPI. Alert numeric defaults vẫn là BA đề xuất/configurable.

Schema bổ sung `RefundItem`, `Inventory.ReservedQuantity`, `Product.IsKeySku/FloorPrice`, `Alert.BaselineValue/ResolutionNote/EscalatedAt`; migration mới duy nhất `SyncFinalGd1BusinessRules`. `Delivered=5` và severity Medium=0/High=1/Critical=2 giữ tương thích persisted integers.

Alert Engine có evaluator riêng, typed config, severity per signal, evidence/baseline, deterministic dedup/cooldown, failure isolation, scheduler, audit và notification abstraction. AL-02 dùng peak windows UTC+7 thật; AL-03 dùng key SKU/Available; AL-04 dùng employee-vs-branch baseline/min sample; AL-05 dùng discount/FloorPrice. AL-04/05 yêu cầu resolve note.

Owner/BranchManager/SystemAdmin chỉ explicit assigned Branch; ChainManager tenant-wide trong Tenant. Store=Branch trong MVP. Rule seed là branch-specific. Web hiển thị 8 KPI, Top/Bottom 10 quantity, inventory availability, Alert detail/note/config và giữ AI Assistant disabled cho GD4.

Validation gần nhất: Build PASS (0 errors, NU1900 environment warning), Unit 53/53 PASS, Integration 36/36 PASS, frontend build PASS/24 modules. Migration apply và SQL Server LocalDB API smoke test PASS, gồm summary/ranking/inventory/drill-down/Alert/RBAC/workflow; Dashboard P95 local 20 mẫu là 84.71 ms. Manual browser UI PASS cho Dashboard/stale state/drill-down, sidebar AI GD4 disabled, BranchManager Alert acknowledge/required-note/resolve, năm rule cấu hình và các state loading/empty/error/permission. Popup `Hosco.Api.exe` `0xe0434352` ngày 2026-09-19 được truy về LocalDB registry access trong sandbox (`0x89C50118`), không có Code Integrity event mới; cùng binary chạy ngoài sandbox PASS. WDAC không bị disable và .NET không được reinstall. Không commit/push/merge.
