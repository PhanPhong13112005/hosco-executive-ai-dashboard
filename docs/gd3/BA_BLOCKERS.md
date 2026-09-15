# BA Blockers GD3

Các quyết định sau chưa có tài liệu BA chính thức. Implementation giữ configurable/provisional và không được dùng làm business truth.

| Chủ đề | Quyết định cần BA chốt | Ảnh hưởng |
|---|---|---|
| Revenue recognition | Status, discount, thời điểm và phân bổ refund | Revenue, trend, AL-02 |
| GMV | Status được tính/loại và gross basis | KPI Summary/drill-down |
| Total Orders | Status và time semantics | Total Orders/AOV/rate |
| AOV | Tử số, mẫu số, zero case và loại trừ | KPI card/drill-down |
| COGS | Quy tắc dùng `UnitCostAtSale` | Gross Profit |
| Return allocation | Phân bổ item/order/refund và kỳ ghi nhận | Gross Profit/Margin/Return Rate |
| Cancellation denominator | Count/value và status denominator | KPI, AL-01 |
| Return denominator | Count/value và partial return | KPI |
| SKU ranking | Revenue/quantity, tie và status | Top/Bottom SKU |
| Dangerous Stock | SafetyStock hay override theo Branch/Product | KPI, AL-03 |
| Peak-hour baseline | Khung giờ, ngày so sánh, seasonality | AL-02 |
| Employee anomaly | Count/rate, nhóm đồng cấp, minimum sample | AL-04 |
| Price/discount anomaly | Giá chuẩn, promotion hợp lệ, dimension SKU | AL-05 |

Khi BA phê duyệt, cần version Metric/Query/Rule contract, cập nhật catalog và test boundary trước khi đổi nhãn khỏi PENDING.

