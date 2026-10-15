# Runbook demo và test
Owner: DEV. Scope validated: Windows PowerShell7, local Mock/SQLite RAM. Production/SQL Server actions cần IT/mentor approval.
## 1. Start và health
Từ root mới clone đúng revision, chạy `./scripts/setup-tester.ps1`. Trong terminal backend:
```powershell
$env:AlertScheduler__RunOnStartup = 'true'
./scripts/run-tester.ps1 -Service Backend -AiProvider Mock -EnableScheduler
```
Trong terminal frontend riêng:
```powershell
./scripts/run-tester.ps1 -Service Frontend
```
Mở http://127.0.0.1:5173; Swagger http://localhost:5000/swagger. Scheduler dùng15min mặc định; RunOnStartup là config demo hợp lệ, không đổi threshold/clock.
```powershell
Invoke-RestMethod http://localhost:5000/health/live
Invoke-RestMethod http://localhost:5000/health/ready
./scripts/verify-gd6.ps1 -DisposableDemo
```
Smoke login 5 tài khoản fixture thuộc 4 role, JWT401/scope403, Mock chat và AL03; không serialize JWT/credentials. Smoke hiện có 33 checks, gồm alert paging với hơn 5 cảnh báo đang mở. Chạy trước suite mutation trên RAM mới. Đừng dùng test runner trên production dù URL local tunnel.
## 2. Stop/reset
Ctrl+C mỗi terminal do mình khởi động. SQLite RAM dừng là hết dữ liệu; khởi động lại tạo demo sạch. Không xóa DB/file/source, không kill process khác, không reset Git. Login lại vì signing key ephemeral thay đổi. Snapshot hiện tại muốn giữ: tải exports và sanitized evidence; không phải database backup có thể restore.
## 3. Full regression
Dừng tester servers trước build.
```powershell
dotnet restore Hosco.slnx
dotnet build Hosco.slnx --no-restore
dotnet test Hosco.slnx --no-build
Push-Location src/Hosco.Web
try { npm.cmd run build } finally { Pop-Location }
node --test tests/Hosco.WebTests/dashboard-formatting.test.mjs tests/Hosco.WebTests/mobile-logout.test.mjs tests/Hosco.WebTests/gd6-handoff.test.mjs
dotnet list Hosco.slnx package --vulnerable --include-transitive
git diff --check
```
Test fake LLM/Telegram không gọi live. Xem [validation](GD6_FINAL_HANDOFF_REPORT.md).
## 4. Persistent SQL Server test (migration/startup/restart đã thử)
Dùng database test mới và Windows integrated auth, không production. Design-time factory đọc ConnectionStrings__HoscoDb từ process, không auto user-secrets/.env. Ngày 14/10/2026, instance local `.\HOSCO` đang chạy và tên `HoscoGd6Qa20261014` được xác nhận chưa tồn tại trước khi tạo. Người clone phải xác nhận instance/tên DB test của mình; không dùng DB chứa dữ liệu cần giữ.
```powershell
$env:ConnectionStrings__HoscoDb = 'Server=.\HOSCO;Database=HoscoGd6Qa20261014;Trusted_Connection=True;TrustServerCertificate=True'
dotnet tool restore --tool-manifest dotnet-tools.json
dotnet tool run dotnet-ef -- migrations list --no-connect --no-build --project src/Hosco.Infrastructure --startup-project src/Hosco.Api
dotnet tool run dotnet-ef -- database update --no-build --project src/Hosco.Infrastructure --startup-project src/Hosco.Api
./scripts/run-tester.ps1 -Service Backend -Database SqlServer -AiProvider Mock
```
list/script generation và database update đã PASS. Startup/restart SQL API được thử bằng `dotnet run --project src/Hosco.Api --no-build --no-launch-profile` trên cổng riêng5088 với Mock, Seed bật, scheduler/Telegram tắt và JWT process key ngẫu nhiên không ghi ra file. Health200, login thật và KPI tháng1 PASS. Sau restart giữ 2.084 unique orders/20rules/5users và Revenue14.022.500/Orders93. Startup tự migrate/seed khi enabled: seed có thể reset rule defaults, không dùng DB tùy chỉnh cần giữ.

Runner `run-tester.ps1` cố định `--urls=http://localhost:5000`; ASPNETCORE_URLS không override command-line. Đừng khởi động server thứ hai ở5000. Dừng server RAM của mình trước khi dùng runner SQL, hoặc dùng direct command với cổng riêng như lần kiểm chứng; JWT/Mock/Telegram vẫn phải cấu hình an toàn. LocalDB có cài nhưng chưa được start/test trong lượt này.
Backup/restore SQL Server **NOT VERIFIED**: thao tác backup đầu tiên bị auto-review từ chối vì đích ghi động chưa được duyệt. Chủ dự án sau đó chọn chưa thực hiện backup/restore; không thử lại và không tạo file backup. Khi được phép ở lượt khác, DBA dùng COPY_ONLY backup ra approved directory, VERIFYONLY rồi restore sang **DB test khác** với reviewed MOVE paths, không WITH REPLACE trên DB hiện có. Không đưa lệnh chưa kiểm chứng như hướng dẫn đã chạy PASS.
## 5. Incident
Lưu timestamp, command exit, safe correlation ID; không dump environment/JWT/request login. Timeout: check ready/API port/proxy/provider config; 401 login lại,403 giữ scope. WDAC: collect CI3033/3077 và Application1026/1000, DLL/path/HRESULT0x800711C7; STOP runtimeblocked, không bypass. [Troubleshooting](TROUBLESHOOTING.md).
