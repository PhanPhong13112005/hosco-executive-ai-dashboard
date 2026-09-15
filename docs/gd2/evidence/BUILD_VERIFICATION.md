# Xác minh Build

## Môi trường

| Hạng mục | Kết quả thực tế |
|---|---|
| SDK | .NET SDK 10.0.400 |
| MSBuild | 18.9.6 |
| Host/Runtime | 10.0.11, win-x64 |
| OS | Windows 10.0.26200 |
| Solution | `Hosco.slnx` |
| Checkpoint | `f75ac38c323ac2b61dd127081cc742852200f197` |

## Lệnh và kết quả

| Lệnh | Kết quả thực tế |
|---|---|
| `dotnet --info` | Hoàn tất; chi tiết SDK/Runtime như trên |
| `dotnet restore` | Exit 0; mọi project đã cập nhật đầy đủ cho Restore |
| `dotnet build` | Exit 0; 0 warnings, 0 errors; thời gian 6.07 giây |

Build output được tạo thành công cho:

- `Hosco.Domain`
- `Hosco.Application`
- `Hosco.Infrastructure`
- `Hosco.Api`
- `Hosco.UnitTests`
- `Hosco.IntegrationTests`

Kết luận: `VERIFIED` (Đã xác minh).
