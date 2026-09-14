# Build Verification

## Environment

| Item | Actual |
|---|---|
| SDK | .NET SDK 10.0.400 |
| MSBuild | 18.9.6 |
| Host/runtime | 10.0.11, win-x64 |
| OS | Windows 10.0.26200 |
| Solution | `Hosco.slnx` |
| Checkpoint | `f75ac38c323ac2b61dd127081cc742852200f197` |

## Commands and results

| Command | Actual result |
|---|---|
| `dotnet --info` | Completed; SDK/runtime details above |
| `dotnet restore` | Exit 0; all projects up-to-date for restore |
| `dotnet build` | Exit 0; 0 warnings, 0 errors; elapsed 6.07 s |

Build output was produced successfully for:

- `Hosco.Domain`
- `Hosco.Application`
- `Hosco.Infrastructure`
- `Hosco.Api`
- `Hosco.UnitTests`
- `Hosco.IntegrationTests`

Verdict: `VERIFIED`.

