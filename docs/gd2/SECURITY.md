# Baseline bảo mật và secret

- Hệ thống xác minh chữ ký JWT Bearer, issuer, audience, lifetime và clock skew 30 giây.
- Credential demo dùng PBKDF2-HMAC-SHA256 với 100,000 vòng lặp; mật khẩu plaintext không bao giờ được lưu trong cơ sở dữ liệu.
- Signing key/connection string đã commit là placeholder rõ ràng cho môi trường phát triển cục bộ. Giá trị production phải đến từ biến môi trường, secret manager hoặc `dotnet user-secrets` khi làm việc local.
- `.gitignore` chặn `.env`, appsettings local, certificate, key và `secrets.json`.
- Phạm vi báo cáo luôn bắt nguồn từ claim đã xác thực, không bao giờ từ Tenant do client cung cấp.
- Truy vấn LINQ của EF Core parameterize bộ lọc người dùng. Cách sắp xếp được chọn từ allow-list trong code; không có raw SQL hoặc endpoint thực thi SQL tùy ý.
- Page size được giới hạn ở 200; request timeout và SQL command timeout là 10 giây.
- Log correlation/request chứa identifier và metadata phạm vi, không chứa mật khẩu, raw JWT, API key, payload email/số điện thoại khách hàng hoặc stack trace bên ngoài Development.
- Lỗi API có dạng `{ code, message, correlationId }`. HTTPS termination và rate limiting phải được áp dụng tại production ingress/API gateway.

## Checklist trước khi phát hành

Xoay vòng JWT signing key, dùng tài khoản SQL theo nguyên tắc đặc quyền tối thiểu, tắt Swagger trừ khi thực sự cần, bổ sung rate limit tại ingress, cấu hình trusted proxy/CORS và chạy dependency/secret/SAST scanning trong CI.
