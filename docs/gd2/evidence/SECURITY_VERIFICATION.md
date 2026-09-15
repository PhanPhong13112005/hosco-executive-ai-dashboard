# Xác minh bảo mật

## Phần triển khai

- JWT validation kiểm tra issuer, audience, lifetime, signing key và clock skew 30 giây.
- Claim được phát hành gồm `sub`, name identifier, `tenant_id`, email, `jti`, một hoặc nhiều role claim và không, một hoặc nhiều `branch_id` claim.
- Reporting policy yêu cầu xác thực và một trong các vai trò `Owner`, `BranchManager`, `ChainManager` hoặc `SystemAdmin`.
- `CurrentUser` là ranh giới chuyển claim thành danh tính. `ReportingScopeFactory` luôn lấy Tenant từ principal đã xác thực, không bao giờ từ query parameter.
- `BranchScopeValidator` kiểm tra Branch thuộc Tenant trước, sau đó kiểm tra quyền trên Branch được phân công, trừ khi vai trò có quyền toàn Tenant.
- Password verification dùng PBKDF2-HMAC-SHA256 với 100,000 vòng lặp và phép so sánh constant-time. Kiểm tra SQL xác nhận 5/5 hash đã lưu có định dạng PBKDF2 dự kiến.

## Truy vết yêu cầu tới test

| Yêu cầu | Minh chứng test chính xác | Kết quả thực tế |
|---|---|---|
| Request báo cáo chưa xác thực trả 401 | `ReportingApiTests.Reporting_without_login_returns_401` | PASS; request thủ công dùng SQL cũng trả 401 |
| Branch Manager đọc được Branch đã phân công | `BranchScopeTests.Branch_manager_can_access_assigned_branch`; `ReportingApiTests.Branch_manager_can_read_assigned_branch_only` | PASS |
| Branch ngoài phân công trả 403 | `BranchScopeTests.Branch_manager_cannot_access_unassigned_branch`; `ReportingApiTests.Branch_manager_is_forbidden_from_other_assigned_tenant_branch` | PASS; request thủ công dùng SQL cũng trả 403 |
| Quản lý toàn Tenant đọc được Branch cùng Tenant | `BranchScopeTests.Chain_manager_can_access_any_branch_in_same_tenant`; `ReportingApiTests.Chain_manager_can_read_branch_in_own_tenant` | PASS |
| Branch khác Tenant bị chặn | `BranchScopeTests.Cross_tenant_branch_is_always_forbidden`; `ReportingApiTests.Cross_tenant_branch_is_forbidden` | PASS |
| Dữ liệu kết quả không vượt Tenant | `ReportingApiTests.Reporting_never_leaks_other_tenant_data` | PASS |
| Client không thể ghi đè Tenant claim | `ReportingApiTests.Client_tenantId_query_parameter_cannot_override_claim` | PASS |
| Login và báo cáo có xác thực hoạt động | Các Integration Test có xác thực cùng request login/đơn hàng thủ công dùng SQL | Login 200; request đơn hàng 200 |

Evidence này không lưu giá trị token hoặc mật khẩu. Token thủ công chỉ được giữ trong bộ nhớ process.

Kết luận: `VERIFIED` (Đã xác minh) cho các luồng xác thực và cô lập Tenant/Branch đã kiểm tra.
