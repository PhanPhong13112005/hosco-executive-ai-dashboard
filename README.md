# HOSCO – Executive AI Dashboard & Smart Alert Chatbot

Nền tảng backend GD2 cung cấp Reporting API an toàn theo Tenant, dùng chung cho Dashboard và Chatbot trong tương lai. Ranh giới truy cập của Chatbot là Reporting API/Danh mục truy vấn (Query Catalog); Chatbot không bao giờ được truy cập trực tiếp cơ sở dữ liệu.

## Công nghệ và cấu trúc

Repository sử dụng .NET 10 vì .NET SDK 10.0.400 là SDK được hỗ trợ và đã cài đặt khi audit workspace ban đầu. Các package được cố định ở bản vá 10.0.11.

```text
src/
  Hosco.Api/             HTTP, JWT, policies, Swagger, middleware, health
  Hosco.Application/     contracts, scope rules, semantic/query catalogs
  Hosco.Domain/          entities and enums
  Hosco.Infrastructure/  EF Core, SQL Server, seed, reporting/audit stores
tests/
  Hosco.UnitTests/
  Hosco.IntegrationTests/
docs/gd2/
```

## 1. Điều kiện tiên quyết

- .NET SDK 10.0.400 hoặc SDK 10.0.x tương thích
- SQL Server, SQL Server Express, container hoặc LocalDB
- Các ví dụ bên dưới dùng PowerShell; có thể sử dụng lệnh tương đương trên shell khác

## 2. Khôi phục package và công cụ

```powershell
dotnet restore Hosco.slnx
dotnet tool restore
```

## 3. Cấu hình cơ sở dữ liệu và secret

Connection string và JWT key được commit chỉ là placeholder cho môi trường phát triển, không phải secret production. Nên ưu tiên user-secrets:

```powershell
dotnet user-secrets init --project src/Hosco.Api
dotnet user-secrets set "ConnectionStrings:HoscoDb" "Server=localhost;Database=Hosco;Trusted_Connection=True;TrustServerCertificate=True" --project src/Hosco.Api
dotnet user-secrets set "Jwt:SigningKey" "replace-with-at-least-32-random-characters" --project src/Hosco.Api
dotnet user-secrets set "Seed:Enabled" "true" --project src/Hosco.Api
```

Hệ thống cũng hỗ trợ các biến môi trường tương ứng với [.env.example](.env.example). File `.env` đã được Git bỏ qua và ứng dụng không tự động nạp file này.

## 4. Áp dụng Migration

```powershell
dotnet tool run dotnet-ef database update --project src/Hosco.Infrastructure --startup-project src/Hosco.Api
```

API cũng gọi `MigrateAsync` khi khởi động. Trong production, thông thường nên chạy Migration như một bước phát hành có kiểm soát trước khi khởi động instance mới.

## 5. Tạo dữ liệu demo xác định

Đặt `Seed:Enabled=true` hoặc chạy bằng profile Development. Trên cơ sở dữ liệu trống, Seed khi khởi động sẽ tạo hai Tenant, bốn Branch, sáu tháng dữ liệu đơn hàng và các anomaly fixture. Lần chạy thứ hai có tính idempotent vì tiến trình dừng khi đã tồn tại dữ liệu Tenant.

```powershell
dotnet run --project src/Hosco.Api --environment Development
```

## 6. Chạy API và Swagger

```powershell
dotnet run --project src/Hosco.Api --environment Development
```

Mở URL do ASP.NET Core hiển thị và thêm `/swagger`. Swagger được bật trong Development và có thể được điều khiển rõ ràng bằng `Swagger:Enabled`.

## 7. Đăng nhập và nhận JWT phát triển

Tất cả tài khoản demo dùng mật khẩu chỉ dành cho môi trường phát triển `HoscoDemo!2026`; cơ sở dữ liệu chỉ lưu hash PBKDF2-SHA256.

| Vai trò | Email | Phạm vi |
|---|---|---|
| Owner | `owner@hosco.local` | Tenant HOSCO-A |
| Branch Manager | `branch.manager@hosco.local` | Chỉ Branch A-HCM |
| Chain Manager | `chain.manager@hosco.local` | Tất cả Branch thuộc HOSCO-A |
| System Admin | `admin@hosco.local` | Quản trị kỹ thuật, Tenant HOSCO-A |
| Dữ liệu kiểm thử cô lập | `owner@fixture.local` | Tenant HOSCO-B |

```powershell
$body = @{ email = "owner@hosco.local"; password = "HoscoDemo!2026" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "http://localhost:5000/api/v1/auth/login" -ContentType "application/json" -Body $body
```

Sao chép `accessToken` vào hộp thoại **Authorize** của Swagger.

## 8. Health Check

```powershell
Invoke-RestMethod http://localhost:5000/health/live
Invoke-RestMethod http://localhost:5000/health/ready
```

`live` không phụ thuộc SQL Server. `ready` kiểm tra cơ sở dữ liệu và chuyển sang trạng thái không khỏe mạnh khi cơ sở dữ liệu không khả dụng.

## 9. Chạy Build và Test

```powershell
dotnet build Hosco.slnx
dotnet test Hosco.slnx --no-build
```

Integration Test khởi chạy HTTP pipeline thực trên cổng local tạm thời với cơ sở dữ liệu quan hệ SQLite in-memory cô lập. Các test này không cần SQL Server.

## Trạng thái định nghĩa nghiệp vụ

GD2/GD3 đã được đồng bộ với Final GD1 Business Spec. KPI-01..08 là `Implemented`, dùng canonical KPI layer và business timezone UTC+7. Các threshold/baseline/window/cooldown của Alert vẫn được ghi rõ là **BA đề xuất / configurable**; xem [báo cáo GD2](docs/gd2/GD2_REPORT.md) và [Alert Catalog](docs/gd3/ALERT_RULE_CATALOG.md).

## Minh chứng xác minh GD2

Bộ audit GD2 có thể tái lập, kết quả Runtime, SQL Migration đã sinh, tài liệu OpenAPI đã thu thập và các giới hạn đã biết được tổng hợp tại [Báo cáo minh chứng GD2](docs/gd2/evidence/GD2_EVIDENCE_REPORT.md). Chạy `./scripts/verify-gd2.ps1` để thực hiện các kiểm tra Build/Test không làm thay đổi source.

## GD3 – Dashboard & Smart Alert

Branch `feature/gd3-dashboard-alert` bổ sung Executive Dashboard, năm Smart Alert rule cấu hình được, scheduler 15–30 phút, persistence, deduplication/cooldown, workflow acknowledge/resolve có note và notification qua structured log. KPI đã final; các giá trị số Alert mang nhãn BA đề xuất/configurable.

Frontend React + TypeScript + Vite nằm tại `src/Hosco.Web`:

```powershell
cd src/Hosco.Web
npm install
npm run dev
```

Đặt `VITE_API_BASE_URL` nếu API không chạy tại `http://localhost:5000`; Vite development server có proxy `/api` mặc định. Production build:

```powershell
npm run build
```

Tài liệu chi tiết nằm trong `docs/gd3`. Môi trường Windows hiện tại có Enterprise WDAC có thể chặn DLL .NET build local trước khi Runtime/Integration Test chạy; xem `docs/gd3/KNOWN_ENVIRONMENT_ISSUES.md`. Trường hợp này phải ghi `BLOCKED_BY_LOCAL_WDAC`, không được ghi Integration Test PASS.
