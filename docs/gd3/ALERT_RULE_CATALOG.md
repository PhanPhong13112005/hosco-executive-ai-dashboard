# Alert Rule Catalog GD3

Semantic của AL-01..AL-05 bám Final GD1. Các số dưới đây là **BA đề xuất / configurable** và được lưu ở `AlertRule`/typed `ConfigJson`; severity được tính theo từng signal.

| Code | Điều kiện MVP | Severity | Window / baseline | Cooldown | Dedup context |
|---|---|---|---|---|---|
| AL-01 | cancellation rate tăng so với trung bình cùng branch | High `>=30%`, Critical `>=50%` | 1 ngày / 7 ngày trước | 4h | Tenant+Branch+Rule |
| AL-02 | revenue trong peak window thấp hơn baseline cùng window | High `<70%`, Critical `<50%` baseline | 11–13, 17–20 UTC+7 / 7 ngày | 2h | Tenant+Branch+Rule+peak |
| AL-03 | `IsKeySku` và `Available=OnHand-Reserved <= SafetyStock` | Medium `<=Safety`, High `<=50% Safety`, Critical `<=0` | near-real-time, 30 phút | 6h | Tenant+Branch+Rule+SKU |
| AL-04 | employee cancel/return rate vượt branch baseline hoặc absolute 15% | Medium trước min sample; High khi N>=10 | 1 ngày / 7 ngày | 1 ngày | Tenant+Branch+Rule+Employee |
| AL-05 | discount `>40%` hoặc sale price dưới FloorPrice | High; Critical khi discount `>60%` | 1 ngày | 1h | Tenant+Branch+Rule+SKU |

Evidence gồm observed/threshold/baseline, branch, entity context và `lastUpdatedAt`. AL-04 không gửi Staff, chỉ BranchManager trong scope được acknowledge/resolve AL-04, và AL-04/AL-05 bắt buộc resolution note. Alert giữ workflow Open → Acknowledged → Resolved, actor/time/note và audit. Rule được seed theo branch để Owner/SystemAdmin chỉ thấy cấu hình trong explicit scope; ChainManager thấy toàn chuỗi.
