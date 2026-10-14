# Xử lý sự cố bàn giao
| Hiện tượng | Kiểm tra an toàn | Phân loại |
|---|---|---|
| NETSDK/tool missing | dotnet --info, global.json10.0.400/latestPatch; tool manifest root explicit | ENVIRONMENT |
| npm.ps1 blocked | npm.cmd; approved PowerShell, không Set-ExecutionPolicy/Bypass | ENVIRONMENT |
| localhost API unreachable | runner dùng5000, Vite proxy5000; plain launch profile5179 là khác origin | SETUP |
| 401 sau restart | key ephemeral/token expiry; logout/login, không attach token | EXPECTED AUTH |
| 403/IDOR404 | current role/tenant/assigned branches, không mở quyền để né | EXPECTED SECURITY |
| today Revenue0 | seed Jan–Jun2026; hỏi tháng1/năm2026 để demo số, không chỉnh đồng hồ | EXPECTED NO DATA |
| AOV/Margin N/A | denominator0 -> API null; totals0 là thật; current stock không historical | EXPECTED SEMANTICS |
| Rule Save400 | check invalid enum/config JSON/bounds; High string valid sau GD5 fix | VALIDATION |
| Vietnamese Ã/Æ/Ä | so decoded API/DB với fresh DOM; valid â không là mojibake | BUG-UI-001 still tester pending |
| no scheduled AL01 | actual 1day/7day baseline, không6actions/5min; current seed historical | PLAN GAP |
| Telegram Skipped | runner disables real channel; mock tests không chứng minh live Sent | NOT VERIFIED LIVE |
| LLM missing/outage | bounded fallback; HTTP200 không chứng minh provider gọi; test fakes | NOT VERIFIED LIVE |
| PDF font missing/clipping | embedded NotoSans resource/license, restore PDFsharp, export regression; không sửa font host | EXPORT |
| NU1902/NU1903 | targeted patched packages10.0.13, audit feed reachable; không suppress audit | SECURITY |
| dotnet crash0xe0434352 | matching Event logs để phân biệt source vs WDAC; không reinstall/disable WDAC | BLOCKED_BY_LOCAL_WDAC nếu DLL bị chặn |
Shared .NET runtime10.0.11 cần IT review/patch theo October13 advisory; pin NuGet không vá toàn bộ machine/runtime. Chưa production sign-off. No blanket npm audit fix or SDK reinstall.
