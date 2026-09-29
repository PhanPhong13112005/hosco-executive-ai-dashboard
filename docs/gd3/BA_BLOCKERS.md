# BA/PENDING còn lại sau Final GD1

Final GD1 đã chốt công thức KPI-01..KPI-08, UTC+7, role/scope và semantic AL-01..AL-05. Các mục cũ về Revenue, GMV, AOV, COGS, return allocation, ranking và dangerous stock không còn là blocker.

Các giá trị số của Alert Catalog vẫn là **BA đề xuất / configurable**, không phải business truth bất biến:

| Chủ đề | Trạng thái hiện tại | Việc cần chốt nếu đưa production |
|---|---|---|
| AL-01 baseline/threshold/escalation | 7 ngày; High 30%, Critical 50%; configurable | seasonality và kênh escalation thật |
| AL-02 peak windows | 11:00–13:00, 17:00–20:00 UTC+7; configurable | lịch lễ/ngày trong tuần |
| AL-03 key SKU | `IsKeySku`, Available/SafetyStock; configurable | nguồn master-data và warehouse/store mapping dài hạn |
| AL-04 employee anomaly | absolute 15%, baseline 7 ngày, min sample 10 | privacy/retention và quy trình điều tra |
| AL-05 price/discount | High 40%, Critical 60%, nullable FloorPrice | promotion whitelist/approval workflow |
| Notification/escalation | logging abstraction trong MVP | recipient directory và kênh production |
| Multi-instance scheduler | single-process MVP | distributed lock/exactly-once delivery |

Ngoài phạm vi: lead-time forecast/auto-PO, fraud scoring, promotion whitelist, batch/shift analysis nâng cao và GD4 chatbot.
