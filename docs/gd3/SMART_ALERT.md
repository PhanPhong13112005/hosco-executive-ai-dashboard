# Smart Alert GD3

## Luồng đánh giá

1. `AlertSchedulerBackgroundService` thức theo interval cấu hình 15–30 phút.
2. `AlertEngine` lấy toàn bộ rule đang bật.
3. Chọn evaluator theo `RuleCode`; mỗi evaluator đọc signal có Tenant/Branch bắt buộc.
4. Evaluator tạo `AlertCandidate` xác định từ giá trị, ngưỡng, entity context và thời điểm do `TimeProvider` cấp.
5. Engine tạo `DedupKey` theo `Tenant:Branch:Rule:Entity` và tìm Alert trong cooldown.
6. Candidate không trùng được persist với status `Open`.
7. `INotificationSender` được gọi; GD3 dùng `LoggingNotificationSender`, không tích hợp Telegram/FCM.
8. Lỗi từng rule được structured log và không làm worker chết.

## Workflow

```text
Open -> Acknowledged -> Resolved
Open ----------------> Resolved
```

Acknowledge và Resolve lưu `UserId` từ JWT cùng timestamp từ `TimeProvider`. Alert đã Resolved không thể quay về Acknowledged. Mutation được ghi `AuditLog`.

## Deduplication và cooldown

`DedupKey` không chứa dữ liệu nhạy cảm và phân biệt Tenant/Branch/entity. Repository kiểm tra `DetectedAt >= now - CooldownMinutes`; trong thời gian này không tạo bản ghi mới. Sau cooldown, cùng signal có thể tạo Alert mới. Scheduler single-instance là phạm vi MVP; production multi-instance cần unique lease/distributed lock bổ sung.

## Configuration

```json
{
  "AlertScheduler": {
    "Enabled": true,
    "IntervalMinutes": 15,
    "RunOnStartup": false
  }
}
```

`RunOnStartup=false` tránh tạo Alert ngoài ý muốn khi API khởi động demo; worker vẫn đánh giá ở tick đầu tiên sau interval. Threshold/baseline/window/cooldown của rule nằm trong database và có thể chỉnh qua API/UI theo role.

