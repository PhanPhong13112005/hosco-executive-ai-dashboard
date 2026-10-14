# Tài khoản demo và phân quyền
Chỉ public Development seed. Credential demo được định nghĩa tại DemoSeed.DemoPassword và có trong hướng dẫn Tester; không dùng hoặc publish credential môi trường thật. Database lưu PBKDF2 hash. Runner generate JWT signing key process-only, không in ra.
| Actual role | Demo email | Effective scope / hạn chế |
|---|---|---|
| Owner | owner@hosco.local | HOSCO-A/A-HCM; configure scoped rules; AL04 action403 |
| BranchManager | branch.manager@hosco.local | HOSCO-A/A-HCM; configure rule403; AL04 workflow trong scope |
| ChainManager | chain.manager@hosco.local | HOSCO-A/A-HCM+A-HN; không HOSCO-B |
| SystemAdmin | admin@hosco.local | HOSCO-A/A-HCM; không cross-tenant superuser |
| Owner fixture | owner@fixture.local | HOSCO-B/B-DN only |
Không Cashier/Staff role enum hoặc account provisioning API trong MVP. Kế hoạch ghi cashier account là gap cần BA/mentor, không tạo role mới chỉ để checklist PASS. Employee/cashier business rows khác authenticated user roles.
Tạo các account demo bằng khởi động backend RAM SeedEnabled=true, không insert SQL thủ công vào production. Tài khoản test mới ngoài5fixture cần quy trình phê duyệt/provisioning riêng.
Sau login gọi scoped branch directory để xác nhận. UI hiện không có badge role/tenant; chứng minh quyền bằng directory và negative API tests, không giả ảnh badge. Logout trước khi đổi role; không lưu JWT vào ảnh/log.
[Smoke](../../scripts/verify-gd6.ps1) checks5directory mappings; HTTP suite covers401/403/404.
