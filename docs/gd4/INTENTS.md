# GD4 Proposed Intents

CHATBOT DOES NOT ACCESS DATABASE DIRECTLY.

No BA Intent Dataset or Conversation Design was found during repository audit. All entries below are **PROPOSED MVP / BA PENDING**, not formally approved business requirements.

| Intent | Supported example | Date required |
|---|---|---|
| KpiOverview | Tổng quan KPI tháng trước? | Yes |
| Revenue | Doanh thu hôm nay? | Yes |
| Gmv | GMV hôm qua? | Yes |
| TotalOrders | Tổng đơn tuần này? | Yes |
| Aov | AOV tuần trước? | Yes |
| GrossProfit | Lợi nhuận tháng này? | Yes |
| GrossMargin | Biên lợi nhuận tháng trước? | Yes |
| CancellationReturnRate | Tỷ lệ hủy tháng trước? | Yes |
| RevenueTrend | Doanh thu 7 ngày gần nhất? | Yes |
| TopProducts | Top 5 sản phẩm bán chạy? | Optional |
| BottomProducts | Bottom 5 sản phẩm bán chậm? | Optional |
| DangerousInventory | Tồn kho nào đang nguy hiểm? | No; current stock |
| CurrentAlerts | Có cảnh báo Critical nào không? | No; Open alerts |

No-date product ranking uses the existing reporting API's unbounded date filter, not an inferred recent period. Result limit defaults to 5 and is capped at 20. Alert severity recognizes Critical, High and Medium. Branch phrases use `chi nhánh CODE/NAME` and resolve exactly against currently accessible branches; arbitrary GUID/tenant inputs are not supported.

## Dates

Supported: hôm nay, hôm qua, tuần này, tuần trước, tháng này, tháng trước, N ngày gần nhất (1–366). Weeks start Monday. Current week/month run to today; past week/month are complete periods. `IBusinessTime` supplies UTC+7 business date and UTC day boundaries; end is next day start minus one tick. KPI/trend without a valid period asks for clarification.

The deterministic resolver is not general NLU. Explicit calendar dates, comparative KPI calculations, conflicting/compound questions and all possible synonyms are outside the proposed MVP. Confidence .95/.72 is a heuristic classification indicator, not a statistically calibrated probability.

## Conversation

`Doanh thu tuần này?` followed by `Còn tuần trước?` reuses Revenue. A standalone supported period also works. Context cannot store/override branch or tenant, and each data request is reauthorized. `Thời tiết hôm nay?` stays Unknown even with previous Revenue context. Clear conversation resets the client context.

## UI suggestions verified

All five initial suggestions have a unit gate and were mapped to supported intents: Revenue, TopProducts, DangerousInventory, CurrentAlerts and RevenueTrend. Unknown returns “Hiện tại tôi chưa hỗ trợ loại câu hỏi này.”; ambiguous Revenue asks for a time range; no data is reported explicitly rather than fabricated.
