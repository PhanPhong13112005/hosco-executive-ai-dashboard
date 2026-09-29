# Kiểm thử GD3 / Final GD1 Sync

Các test bao phủ canonical KPI (discount, cancelled, full/partial return, GMV trước discount, AOV/null, COGS/GP/GM, distinct cancel-return denominator), UTC+7 boundary, severity boundary AL-01..05, per-signal severity/baseline, cooldown/dedup, workflow actor/time/note và resolve-note AL-04/05.

Security test bao phủ Owner/BranchManager/SystemAdmin không truy cập branch chưa gán, ChainManager tenant-wide trong tenant, cross-tenant 403, BranchManager config 403 và IDOR Alert. Persistence test giữ Product Ranking materialize-before-group để không tái tạo lỗi SQL Server navigation GroupBy.

Kết quả validation gần nhất trong working tree:

- `dotnet build Hosco.slnx --no-restore`: PASS, 0 errors, 1 NU1900 môi trường.
- `dotnet test Hosco.slnx --no-build`: Unit 53/53, Integration 36/36 PASS.
- `npm.cmd run build`: PASS, TypeScript + Vite, 24 modules.
- SQL Server LocalDB: migration từ database trống và API runtime smoke PASS.
- Manual browser UI: PASS cho login, Dashboard/KPI/stale state/drill-down, Top/Bottom, dangerous inventory, Alert Center acknowledge + required note + resolve, năm rule AL-01..05 và AI Assistant disabled GD4. Các state được quan sát trực tiếp: loading; empty ở kỳ 2028 không có giao dịch (`AOV`/margin `N/A`, trend và ranking báo không có dữ liệu); error khi API dừng (`Không thể kết nối API` + `Thử lại`); permission/403 khi BranchManager lưu alert configuration.
- NFR smoke trên LocalDB demo: 20 request `dashboard/summary`, P95 `84.71 ms` (min `59.52 ms`, median `66.08 ms`, max `126.69 ms`), dưới target 3 giây; đây là local smoke, không thay thế production load test.
- Runtime RBAC cuối: unauthenticated 401, BranchManager truy cập branch khác 403, Owner thao tác AL-04 403, ChainManager thấy 10 rule toàn chuỗi và SystemAdmin chỉ thấy 5 rule của branch được gán.

LocalDB/SQL Server và manual browser verification chỉ được ghi PASS khi thực sự chạy. Nếu Code Integrity chặn DLL trước startup, dùng `BLOCKED_BY_LOCAL_WDAC`, không ghi application defect/PASS; xem `KNOWN_ENVIRONMENT_ISSUES.md`. Popup CLR `0xe0434352` không tự nó chứng minh WDAC: lần kiểm tra 2026-09-19 cho thấy một popup `Hosco.Api.exe` có nguyên nhân cụ thể là LocalDB không truy cập được instance registry trong sandbox (`SqlException` error 50, inner Win32 `0x89C50118`); cùng binary chạy ngoài sandbox đã startup và phục vụ UI thành công, không có Event 3033/3077 mới.
