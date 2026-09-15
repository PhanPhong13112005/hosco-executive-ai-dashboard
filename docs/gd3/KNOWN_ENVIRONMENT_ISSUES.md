# Known Environment Issues – GD3

## BLOCKED_BY_LOCAL_WDAC: local API runtime và Integration Test (intermittent)

### Hiện tượng

Trên máy Windows dùng để triển khai GD3, `dotnet build Hosco.slnx` hoàn tất thành công nhưng tiến trình `dotnet` con do Integration Test khởi động không thể load DLL của ứng dụng. Windows từng hiển thị `dotnet.exe - Application Error` với exception `0xe0434352`.

### Bằng chứng Event Log

- `.NET Runtime`, Event ID `1026`: tiến trình kết thúc vì `System.IO.FileLoadException` khi load `Hosco.Api.dll`; HRESULT `0x800711C7`.
- `Application Error`, Event ID `1000`: `dotnet.exe`, exception code `0xe0434352`, module báo lỗi `KERNELBASE.dll`.
- `Microsoft-Windows-CodeIntegrity/Operational`, Event ID `3033` và `3077`: Enterprise Code Integrity policy `{0283ac0f-fff1-49ae-ada1-8a933130cad6}` chặn DLL build cục bộ vì không đạt Enterprise signing level.
- `Get-AuthenticodeSignature` xác nhận `Hosco.Api.dll` là `NotSigned`, đúng với output Debug .NET được build cục bộ. File không có `Zone.Identifier`.
- Event lịch sử cũng ghi nhận cùng policy từng chặn `Hosco.Application.dll` và `Hosco.Domain.dll`; đây không phải lỗi phát sinh từ business logic GD3.

### Trạng thái xác minh

| Hạng mục | Kết quả |
|---|---|
| .NET SDK | 10.0.400 |
| .NET Runtime / ASP.NET Core | 10.0.11 |
| Build | PASS, 0 warning, 0 error tại thời điểm audit |
| Unit Test GD2 | 11/11 PASS tại thời điểm audit |
| Integration Test tại thời điểm incident | `BLOCKED_BY_LOCAL_WDAC`; không được ghi nhận PASS cho lần chạy bị chặn |
| Local API runtime tại thời điểm incident | `BLOCKED_BY_LOCAL_WDAC`; application bị chặn trước khi startup hoàn tất |

### Xác minh lại ngày 2026-09-15

Không thay đổi WDAC, không ký lại DLL và không sửa source để né policy. Trong lần xác minh sau trên cùng working tree:

- `dotnet test Hosco.slnx --no-build`: 24/24 Unit Test và 26/26 Integration Test PASS;
- EF Core xác nhận không có model change chưa được migration;
- migration GD3 apply LocalDB thành công và schema được kiểm tra;
- API Development khởi động được; login, dashboard, alert workflow và out-of-scope protection được kiểm tra thành công.

Kết quả sau không biến lần chạy bị Code Integrity chặn thành PASS. Nó cho thấy enforcement trên môi trường local có tính intermittent. Nếu Event 3033/3077 và `0x800711C7` tái diễn trước application startup, lần xác minh đó phải tiếp tục được ghi là `BLOCKED_BY_LOCAL_WDAC`.

### Kết luận và nguyên tắc xử lý

Đây là hạn chế của môi trường Windows Application Control / WDAC, không phải application defect khi Event Log chứng minh DLL bị Code Integrity chặn trước khi application chạy. GD3 không sửa source để né policy, không disable WDAC/Code Integrity và không reinstall .NET.

Cách xử lý an toàn, nếu incident tái diễn ổn định, là nhờ administrator cho phép output phát triển của repository theo policy tổ chức, cung cấp certificate ký tin cậy, hoặc cung cấp môi trường phát triển được phê duyệt. Chỉ được ghi PASS cho từng command theo output thực tế của chính lần chạy đó.
