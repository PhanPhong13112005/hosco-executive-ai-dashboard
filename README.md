# HOSCO Executive AI Dashboard & Smart Alert Chatbot

MVP điều hành bán lẻ gồm Dashboard KPI, Smart Alert và trợ lý báo cáo tiếng Việt. Chatbot chỉ lấy dữ liệu qua Reporting API có JWT/RBAC/Tenant/Branch guard, không truy cập database trực tiếp hoặc thực thi SQL do LLM sinh.

Bản bàn giao GD6 được chuẩn bị trên `fix/gd5-tester-findings`. Baseline GD5 đã công bố là `84b9414`; các commit GD6 chỉ local cho đến khi DEV được phép push. Kiểm tra revision do DEV cung cấp trước khi dùng kết quả kiểm thử GD6. [Báo cáo GD6](docs/gd6/GD6_FINAL_HANDOFF_REPORT.md) ghi kết quả mới và các mục còn pending.

## Chạy demo từ một clone mới

Điều kiện: Git, PowerShell7 được công ty cho phép, .NET SDK10.0.400 hoặc compatible10.0.4xx patch theo global.json, Node20.19+ trong nhánh20 hoặc >=22.12, npm và truy cập NuGet/npm cho lần restore đầu. Không cần SQL Server, API key hay Telegram token cho demo Mock. Runtime phải được IT duyệt bản vá trước khi triển khai đối ngoại.

```powershell
git clone --branch fix/gd5-tester-findings --single-branch https://github.com/PhanPhong13112005/hosco-executive-ai-dashboard.git
Set-Location hosco-executive-ai-dashboard
git branch --show-current
git rev-parse HEAD
git status --short
./scripts/setup-tester.ps1
```

Setup chạy restore, `dotnet tool restore --tool-manifest dotnet-tools.json`, backend build, lockfile-based `npm.cmd ci` và frontend build. Không đổi execution policy, production database hay Git branch. Không dùng `dotnet tool restore` mặc định vì tool manifest nằm ở root, không phải .config. Nếu policy chặn script, dùng các [manual commands](docs/testing/TESTER_SETUP.md) trong terminal được IT phê duyệt, không bypass WDAC.

Terminal backend (root):

```powershell
$env:AlertScheduler__RunOnStartup = 'true'
./scripts/run-tester.ps1 -Service Backend -AiProvider Mock -EnableScheduler
```

Terminal frontend riêng (root, không kế thừa credential backend):

```powershell
./scripts/run-tester.ps1 -Service Frontend
```

Web: http://127.0.0.1:5173/; API: http://localhost:5000/; Swagger: http://localhost:5000/swagger; live/ready: `/health/live`, `/health/ready`. Vite proxy gọi5000; plain launch profile5179 không phải quick-start này. StrictPort không tự chuyển sang cổng khác. Dừng bằng Ctrl+C từng server của mình.

## Dữ liệu và tài khoản

SQLite demo là relational RAM, không file persistent. API gọi EnsureCreated và DemoSeed; stop/restart mất các rule edits, alert workflow và delivery/audit vừa tạo. Không gọi SQL Server migration cho chế độ này.

Seed giả lập hai tenant, bốn branch, năm user, bốn role và sales/anomaly lịch sử Jan–Jun2026. Repeated seed bổ sung final fixtures và áp rule defaults, không chỉ là no-op. Chỉ dùng database test riêng, không production hoặc DB có config cần giữ.

| Role | Public demo email | Quyền thực tế |
|---|---|---|
| Owner | owner@hosco.local | HOSCO-A, A-HCM only; AL04 action403 |
| BranchManager | branch.manager@hosco.local | A-HCM; rule config403 |
| ChainManager | chain.manager@hosco.local | A-HCM+A-HN, không tenantB |
| SystemAdmin | admin@hosco.local | HOSCO-A/A-HCM; không cross-tenant superuser |
| Owner fixture | owner@fixture.local | HOSCO-B/B-DN only |

Public Development credential được định nghĩa tại `DemoSeed.DemoPassword` (có trong TESTER_SETUP), không phải mật khẩu môi trường thật. DB chỉ lưu PBKDF2 hash. Logout giữa các tài khoản. Không thêm cashier authrole: employee/cashier business rows khác authenticated roles.

Để demo số liệu: chọn 01/01–31/01/2026, HCM; Revenue14,022,500, GMV14,062,500, Orders93. Câu “Doanh thu hôm nay thế nào?” vào tháng10 có thể đúng là no-data. Không chỉnh clock/seed/formula để tạo câu trả lời đẹp. AL03 có thể phát sinh thật từ current low stock khi scheduler chạy; “6 hủy/5 phút” chưa khớp rule AL01 và chưa có cancellation-write API.

## Kiểm thử

Dừng tester servers trước build; Integration suite tự tạo Kestrel/SQLite/Mock trên cổng riêng.

```powershell
dotnet build Hosco.slnx --no-restore
dotnet test Hosco.slnx --no-build
Push-Location src/Hosco.Web
try { npm.cmd run build } finally { Pop-Location }
node --test tests/Hosco.WebTests/dashboard-formatting.test.mjs tests/Hosco.WebTests/mobile-logout.test.mjs tests/Hosco.WebTests/gd6-handoff.test.mjs
git diff --check
```

Khi demo server mới đang chạy: `./scripts/verify-gd6.ps1 -DisposableDemo` kiểm tra HTTP thật và chỉ in sanitized assertions. Python GD5 extended runner là optional, cần openpyxl và workbook Tester gốc ngoài repo; không phải prerequisite để mở demo.

## Cấu hình và bảo mật

[.env.example](.env.example) là mẫu, **.NET và scripts không tự load .env/appsettings.Local.json**. Project chưa có UserSecretsId, không yêu cầu user-secrets init như bước bàn giao. Cấu hình thật dùng process environment hoặc secret manager đã được phê duyệt; không sửa csproj để giả định auto-loading.

Runner sinh JWT key ephemeral khi chưa có key; không in giá trị. Key đổi cần login lại. Issuer/audience dùng Hosco.Api/Hosco.Clients; API kiểm chữ ký và lifetime. Never commit key/token/password thật, không VITE_* cho secret backend.

AI mặc định Mock/local deterministic intent, không tạo số liệu báo cáo. OpenAI/Gemini adapters chỉ trả structured intent; key/model backend-only, bounded timeout/retry, fallback và API guard. Live provisioning, privacy/cost/model acceptance chưa xác minh. [LLM provider](docs/gd4/LLM_PROVIDER.md), [live smoke](docs/testing/LIVE_AI_SMOKE_TEST.md).

Telegram bị runner tắt, delivery hiển thị Skipped; fake tests cover retry/backoff/idempotency nhưng không chứng minh real delivery. Scheduler15–30min; demo dùng RunOnStartup config, không thay business rules.

Crypto dependencies Pkcs/Xml được pin10.0.13 theo [security patch evidence](docs/gd6/SECURITY_PATCH.md). Shared runtime máy hiện tại chưa được vá trong lượt này; package audit sạch không tương đương production security sign-off.

## SQL Server persistent test

Tùy chọn, chỉ database test mới. Design-time factory đọc `ConnectionStrings__HoscoDb`; tool manifest phải restore explicit. API startup dùng MigrateAsync. Commands và trạng thái offline/runtime/backup tại [runbook](docs/gd6/OPERATIONS_RUNBOOK.md); GD6 local không coi SQL script generation là database migration PASS.

## Kiến trúc và tài liệu

- [Kiến trúc/ADR](docs/gd6/ARCHITECTURE_OVERVIEW.md), [ADR index](docs/gd6/ADR_INDEX.md).
- [Data/ERD/KPI/Query](docs/gd6/DATA_DOCUMENTATION.md), [demo data](docs/gd6/DEMO_DATA_GUIDE.md), [accounts](docs/gd6/DEMO_ACCOUNTS_GUIDE.md).
- [Demo script](docs/gd6/DEMO_SCRIPT.md), [rehearsal](docs/gd6/DEMO_REHEARSAL_REPORT.md), [known issues](docs/gd6/KNOWN_ISSUES.md).
- [GD2 evidence](docs/gd2/evidence/GD2_EVIDENCE_REPORT.md), [GD3](docs/gd3/GD3_REPORT.md), [GD4](docs/gd4/GD4_REPORT.md), [GD5](docs/testing/GD5_SELF_QA_REPORT.md).
- [BA pending](docs/testing/BA_DECISIONS.md), [acceptance notes](docs/gd6/GD6_ACCEPTANCE_NOTES.md).

Source chia Domain/Application/Infrastructure/Api/Web. KPI layer canonical phục vụ Reporting/Alerts; Chat là HTTP consumer. UI có8cards và Top/Bottom riêng. Không tự chuyển BA decisions/defaults/PENDING thành approved hoặc nhận mentor đã nghiệm thu.

Báo cáo cá nhân, dữ liệu định danh sinh viên và file Word hiện có không đưa vào Git. Đây là DEV handoff cho test/review, chưa nghiệm thu production.
