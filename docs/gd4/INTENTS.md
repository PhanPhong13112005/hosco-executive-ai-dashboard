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

Supported: hôm nay, hôm qua, tuần này, tuần trước, tháng này (including tháng nì), tháng trước, N ngày gần nhất (1–366), and explicit `tháng M năm YYYY` (month 1–12, year 2000–2100). A numbered month without a year asks for the year rather than assuming one. `dt` is recognized as a whole-word revenue abbreviation. Weeks start Monday. Current week/month run to today; past week/month are complete periods. `IBusinessTime` supplies UTC+7 business date and UTC day boundaries; end is next day start minus one tick. KPI/trend without a valid period asks for clarification.

The deterministic resolver is not general NLU. Arbitrary calendar dates, comparative KPI calculations, conflicting/compound questions and all possible synonyms remain outside the proposed MVP. Explicit unsupported employee/customer/cash-flow dimensions, comparisons, per-branch grouping, SKU-specific filters and revenue-based SKU ranking return a non-reporting scope-gap explanation instead of silently dropping the requested dimension. Confidence .95/.72 is a heuristic classification indicator, not a statistically calibrated probability.

## Conversation

`Doanh thu tuần này?` followed by `Còn tuần trước?`, `Còn hôm qua thì sao?` or `Còn tháng trước thì sao?` reuses Revenue. A standalone supported period also works. Context cannot store/override branch or tenant, and each data request is reauthorized. `Thời tiết hôm nay?` stays Unknown even with previous Revenue context. Clear conversation resets the client context. Natural `của Hà Nội`/`của HN` branch references are resolved only against the current authorized directory (including HOSCO display names and HN/HCM code aliases); inaccessible references still return 403.

## UI suggestions verified

All five initial suggestions have a unit gate and were mapped to supported intents: Revenue, TopProducts, DangerousInventory, CurrentAlerts and RevenueTrend. Unknown returns “Hiện tại tôi chưa hỗ trợ loại câu hỏi này.”; ambiguous Revenue asks for a time range; no data is reported explicitly rather than fabricated.
