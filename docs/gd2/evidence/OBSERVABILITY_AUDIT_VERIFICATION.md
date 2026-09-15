# Xác minh Observability và audit

## Correlation và request logging

`CorrelationMiddleware` chấp nhận giá trị không rỗng dài tối đa 128 ký tự, chỉ gồm chữ cái, chữ số, dấu gạch ngang, gạch dưới hoặc dấu chấm. Giá trị thiếu/không hợp lệ được thay bằng GUID 32 ký tự và mọi response đều nhận `X-Correlation-ID`.

Request log có cấu trúc chứa path, status, số mili giây xử lý, Correlation ID, User ID, Tenant ID, branch claim và Query ID. Log không chứa request body, Authorization header, raw JWT, mật khẩu hoặc API key. `Valid_correlation_id_is_reused` đã PASS và lần xác minh Runtime thủ công cũng tái sử dụng ID được cung cấp.

## Ánh xạ exception

| Exception | HTTP/code |
|---|---|
| `ValidationException` | 400 / `validation_error` |
| `ForbiddenException` | 403 / `forbidden` |
| `KeyNotFoundException` | 404 / `not_found` |
| `BusinessDefinitionPendingException` | 409 / `business_definition_pending` |
| `OperationCanceledException` do request bị hủy | 499 / `request_cancelled` |
| Loại khác | 500 / `internal_error` |

Response chứa code, message và Correlation ID. Với lỗi chưa được xử lý, message của exception chỉ được trả trong Development; production trả message chung. Logger ghi chi tiết exception ở phía server.

## Audit baseline

`AuditLog` và `IAuditWriter`/`AuditWriter` là infrastructure thực. Writer lưu Tenant, người dùng hiện tại, Branch/resource/query tùy chọn, Correlation ID, metadata đã serialize và timestamp. Endpoint orders gọi writer cho `orders.list.v1`; lần xác minh dùng SQL đã tạo một dòng.

- Infrastructure tồn tại: `VERIFIED` (Đã xác minh).
- Được dùng ở endpoint danh sách đơn hàng: `VERIFIED` (Đã xác minh).
- Bao phủ audit toàn bộ nghiệp vụ/báo cáo: `NOT VERIFIED` (Chưa xác minh) — các reporting endpoint khác không gọi writer.

Kết luận tổng thể: `PARTIALLY VERIFIED` (Xác minh một phần).
