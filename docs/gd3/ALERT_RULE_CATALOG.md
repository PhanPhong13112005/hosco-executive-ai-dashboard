# Alert Rule Catalog GD3

Tất cả threshold/baseline dưới đây là cấu hình demo có thể thay đổi. `BA status = PENDING`; không có giá trị nào được trình bày như business truth.

| Code | Tên | Mục tiêu | Nguồn dữ liệu | Phạm vi | Điều kiện preview | Threshold | Baseline | Window | Severity | Cooldown | Dedup strategy | Status | BA status |
|---|---|---|---|---|---|---|---|---:|---|---:|---|---|---|
| AL-01 | Tỷ lệ hủy đơn bất thường | Phát hiện tăng hủy | Orders | Tenant/Branch | cancellation rate `>=` config | 20% demo | null | 10,080 phút | Warning | 1,440 phút | Tenant+Branch+Rule | Configurable | PENDING |
| AL-02 | Doanh thu giờ cao điểm giảm | Phát hiện sụt doanh thu | Orders | Tenant/Branch | revenue drop `>` config | 30% demo | fixed config hoặc cửa sổ trước | 10,080 phút | Critical | 1,440 phút | Tenant+Branch+Rule | Configurable | PENDING |
| AL-03 | SKU quan trọng tồn kho thấp | Phát hiện thiếu hàng | Inventory/Product | Tenant/Branch/SKU | quantity `<=` threshold/safety stock | 8 demo | null | 15 phút | Critical | 720 phút | Tenant+Branch+Rule+Product | Configurable | PENDING |
| AL-04 | Nhân viên có hủy/hoàn bất thường | Phát hiện thao tác bất thường | Orders/Employee | Tenant/Branch/Employee | cancel+return count `>=` config | 5 demo | null | 10,080 phút | Warning | 1,440 phút | Tenant+Branch+Rule+Employee | Configurable | PENDING |
| AL-05 | Giá bán hoặc giảm giá bất thường | Phát hiện discount cao | Orders | Tenant/Branch/Order | discount rate `>=` config | 25% demo | null | 10,080 phút | Warning | 1,440 phút | Tenant+Branch+Rule+Order | Configurable | PENDING |

AL-02 được đặt tên theo yêu cầu MVP nhưng dữ liệu hiện tại chưa có dimension “giờ cao điểm” được BA phê duyệt; implementation dùng cửa sổ cấu hình và được đánh dấu provisional. AL-05 hiện kiểm tra discount cấp đơn hàng; quy tắc price deviation vẫn là BA blocker.

