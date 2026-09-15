# Kiểm thử GD3

## Baseline

Trước khi sửa source GD3, Restore và Build PASS; 11 Unit Test + 17 Integration Test GD2 = 28/28 PASS trong một lần chạy ngoài sandbox. Sau đó Windows Enterprise Code Integrity bắt đầu chặn ngẫu nhiên DLL local; xem `KNOWN_ENVIRONMENT_ISSUES.md`.

## Unit Test

`tests/Hosco.UnitTests` bổ sung test cho:

- năm evaluator AL-01..AL-05;
- threshold boundary (`>=`, `>`, `<=`);
- disabled rule và thời gian truyền từ `TimeProvider`;
- dedup/cooldown trước và sau hạn;
- khác Tenant/Branch không dedup nhầm;
- một rule lỗi không dừng rule còn lại;
- acknowledge/resolve lưu actor/time và transition không hợp lệ;
- quyền update rule và Alert filter validation.

Kết quả gần nhất: 24/24 PASS (11 GD2 + 13 GD3), 0 failed, 0 skipped.

## Integration Test source

`DashboardAndAlertApiTests` kiểm tra authentication Dashboard, Branch filter/403, invalid date, supporting endpoints, Alert 401, list/detail/acknowledge/resolve, Tenant/Branch isolation và role-protected rule config. `AlertEnginePersistenceTests` dùng SQLite quan hệ thật để kiểm tra rule tạo Alert, persist, notification và suppress lần chạy thứ hai.

Targeted persistence result: 1/1 PASS. Lần chạy full gần nhất ngày 2026-09-15: 26/26 Integration Test PASS (17 GD2 + 9 GD3), 0 failed, 0 skipped. Nếu WDAC chặn một lần chạy khác trước startup, lần đó phải ghi `BLOCKED_BY_LOCAL_WDAC`, không suy diễn thành application defect hoặc PASS.

Tổng lần xác minh cuối: 50/50 PASS gồm 24 Unit Test và 26 Integration Test. Restore/Build còn cảnh báo môi trường `NU1900` vì NuGet vulnerability feed không truy cập được; compile có 0 error.

## Lệnh

```powershell
dotnet build Hosco.slnx --no-restore
dotnet test tests/Hosco.UnitTests/Hosco.UnitTests.csproj --no-build
dotnet test tests/Hosco.IntegrationTests/Hosco.IntegrationTests.csproj --no-build

cd src/Hosco.Web
npm install
npm run build
```

Migration SQL được generate, migration GD3 apply LocalDB thành công, EF xác nhận model không có thay đổi chưa được migration. Runtime API đã xác minh login `200`, unauthenticated `401`, dashboard `200`, alert rules/list/workflow thành công và Branch ngoài scope `403`. WDAC vẫn là known intermittent environment issue; chỉ công bố kết quả theo output thực tế.
