# Bàn giao GD6 – chuẩn bị hoàn thiện và nghiệm thu

Ngày thực hiện: 14/10/2026. Branch **fix/gd5-tester-findings**. Baseline GD5/origin đã đối chiếu: **84b94141bb9afc1a1a3cb16e58e3dcc2979129e3**. GD6 tạo các commit local theo bốn nhóm; lấy HEAD cuối bằng `git rev-parse HEAD` sau commit hồ sơ. Không tự ghi hash của commit chứa chính tài liệu này.

Main được giữ nguyên tại ed930682ac7b56f919fc0971535c19565bcd7a30. Không reconcile với origin/main, không merge/reset/rebase/restore/cherry-pick, không push GD6.

## 1. Checklist 5 nhiệm vụ theo kế hoạch

Bản workbook không có hậu tố(1) đã được chủ dự án xác nhận. Đối chiếu sheet GD6, dòng5–9; không sửa workbook hay chuyển tuần tương lai thành việc đã làm. Thực tập chính thức vẫn14/09–14/11/2026; mốc GD6 kế hoạch09–14/11 là tương lai tại ngày chạy này.

| Nhiệm vụ | Trạng thái | Kết quả / giới hạn |
|---|---|---|
| 1 README/API/env/setup/migration/seed | DONE | Hướng dẫn sửa theo source; setup clone, API thật và Swagger tested; không có secret mới |
| 2 ADR/runbook/ERD/data/KPI/query docs | DONE trong phạm vi chuẩn bị tài liệu | Source và BA references đã đối chiếu; migration/startup/restart SQL test PASS. Backup/restore NOT VERIFIED theo lựa chọn của chủ dự án |
| 3 Demo data và accounts thực tế | DONE | Seed tái lập, duplicate guards, 5 accounts/4 roles, tenant/branch negative tests; không thêm cashier role |
| 4 Demo Dashboard/Alert/AI/export/logout | PARTIAL | Luồng triển khai local được diễn tập thật. “6 hủy/5phút” không khớp Catalog/source; AL03 alternative chờ BA/mentor duyệt. Live outage chưa thử |
| 5 Feedback mentor/known issues/backlog/team/report | PENDING APPROVAL | Hồ sơ và nội dung báo cáo đã chuẩn bị; chưa có buổi mentor demo, feedback hoặc sign-off |

**3/5 nhiệm vụ DONE ở phạm vi chuẩn bị DEV; 1 PARTIAL; 1 PENDING APPROVAL.** Bộ tài liệu phục vụ cả5/5 nhiệm vụ đã có, không đồng nghĩa5/5 đã nghiệm thu. Không tuyên bố production-ready.

## 2. Validation mới, không tái sử dụng số baseline như kết quả hiện tại

Các gates dưới đây chạy thực tế trên Windows trusted process hiện tại, Mock/local databases, không tắt WDAC hoặc reinstall.NET.

| Gate | Kết quả |
|---|---|
| Backend build `dotnet build Hosco.slnx --no-restore` | PASS, 0 warnings/0 errors |
| Unit | 180/180 PASS, 0 fail/skip |
| Integration | 167/167 PASS, 0 fail/skip; tăng3 checks GD6 từ baseline164 |
| Frontend npm build | PASS |
| Frontend regression | 7/7 PASS, 0 fail/skip; 4 application checks +3 GD6 handbook/security/link checks |
| HTTP extended regression | 167/167 PASS, 146 requests, 28 rows workbook gốc observed; 26 current MVP contracts, 13 positive intents canonical |
| GD6 smoke | 33/33 PASS, gồm thực tế scheduler tạo AL03 và alert paging >5 |
| Export content checks | 15/15 PASS; UI PDF/XLSX cũng render/read-only inspect |
| Health/OpenAPI | live/ready200;21 published paths; Bearer scheme và demo endpoints tested |
| Migration SQL | 4/4 áp database test mới; offline list/idempotent script cũng PASS |
| SQL seed/runtime/restart | PASS:2tenant/4branch/5users/20rules/2.084 unique orders; tháng1Revenue14.022.500/Orders93 giữ sau restart |
| NuGet vulnerability audit | Fresh feed không báo vulnerable packages trong6 project graphs; crypto pins10.0.13 |
| Git whitespace | `git diff --check` PASS; final staged diff cũng kiểm trước commit |

HTTP suite cover BUG-PAGE-01, BUG-EMPTY-01, AI019/020, JWT invalid/unauthenticated401, cross-tenant/branch403, IDOR404, BranchManager config403, Owner AL04 action403, allowed workflow, configSave, exports và13 current intents. AI005/038 vẫn **BA_PENDING_ORACLE**, không cộng thành BA accuracy PASS. Workbook gốc checksum a78c2c9a6bbcd5eaea0d3910a7ffcee2bd643802471870a46f3a92653a9fc0f4 khớp baseline, không sửa tài liệu Tester.

### Clean clone

Baseline84b9414 được clone local `--no-hardlinks --single-branch`, setup script thực hiện restore/tool restore/backend build/npm ci/frontend build PASS, nhưng phát hiện9 crypto audit warnings mới. Clone bản GD6 source/tests tại **6e5e411d832b76db972adc04a78c7257d31cf3d9** đã chạy đúng setup script: restore/tool restore/build0warnings/npm ci/npm build PASS, Unit180/180, Integration167/167 và4 frontend application checks PASS. Backend/frontend được start bằng README scripts từ clone, smoke33/33 PASS; clone working tree sạch sau setup. Ba handbook checks chạy PASS ở root với đủ19docs cuối; commit sau source/tests chỉ thêm tài liệu/evidence, không thay production code. Không yêu cầu file ngoài Git để mở demo; workbook Tester chỉ cần cho extended optional regression.

### Lần chạy trung gian và nguyên nhân

- Test GD6 mới ban đầu giả định seed có NotificationDeliveries; source seed chưa dispatch nên count0 là đúng. Test mới được sửa precondition, vẫn assert counts/IDs/scope và count0. Không sửa application hoặc regression cũ để che lỗi.
- HTTP đầu tiên166/167: harness so Chat AlertList pageSize5 với direct API mặc định50. Scheduler tăng Open từ4 lên6 nên hai trang khác nhau. Sửa **request đối chiếu** cùngpageSize5, giữ strict equality, thêm smoke assert>5 total/5items/cùngscope. Không bỏ assertion, không sửa Chatbot.
- Thử server SQL bằng runner trong lúc5000 đã chạy sinh bind exception10048: command-line URLs của runner thắng ASPNETCORE_URLS. Sau dùng direct command5088, startup/restart PASS. Đây là lỗi thao tác setup, không phải WDAC hay KPI defect.

## 3. Runtime/WDAC evidence và các giới hạn

Trusted build/tests/API runtime chạy được. Event Viewer từ15:07 UTC+7 có Application1026/1000 lúc15:41:56–57 cho Hosco.Api.exe với `AddressInUseException`/Socket10048, cổng5000; không có CodeIntegrity3033/3077 mới trong khoảng này. Đây không chứng minh policy WDAC toàn máy đã tắt hoặc được sửa. Historical0x800711C7 unsigned DLL issue vẫn là môi trường; không source workaround.

Backup/restore **NOT VERIFIED**: lần tạo backup bị auto-review từ chối do đích ghi động chưa duyệt; chủ dự án chọn chưa thực hiện. Không tạo backup, không restore, không retry né kiểm soát. Database test HoscoGd6Qa20261014 được giữ riêng; không xóa DB/source.

Live OpenAI/Gemini/Telegram, load/P95/SLA và production rollout **NOT VERIFIED**. Fake HTTP provider/Telegram tests không chứng minh live delivery/model quality. NuGet pins không vá shared .NET/ASP.NET runtime10.0.11; cần IT/runtime update approval. Không tự reinstall SDK, disable WDAC, gửi dữ liệu ra LLM hoặc dùng token thật.

BA-GD5-01..10 vẫn pending; source KPI không bị thay đổi. BUG-UI-001 hiển thị đúng ở fresh demo, nhưng chưa có retest độc lập trên DB/build gốc của Tester; không tự đóng ticket. Cashier auth role và UI role badge không tồn tại; không giả evidence. Drill-down/empty-state GD6 kiểm API/export/regression, chưa click/chụp UI mới cho hai mục này.

## 4. Thay đổi và bằng chứng

- Root: README.md, .env.example, .gitattributes; docs/testing/TESTER_SETUP.md.
- Source: Hosco.Infrastructure.csproj pin Pkcs/Xml10.0.13; Program.cs chỉ cập nhật Swagger metadata, không đổi production config/KPI.
- Tests: Gd6DemoReadinessTests.cs (2), Gd6OpenApiTests.cs (1), gd6-handoff.test.mjs (3); scripts/verify-gd6.ps1; sửa canonical page query của Verify-Gd5SelfQa.py.
- Tài liệu GD6: readiness audit, architecture, ADR, operations, troubleshooting, data, demo accounts/data/script/rehearsal, acceptance checklist/notes, known issues, future backlog, team contributions, report content, project summary, security patch và báo cáo này:19 Markdown files.
- Evidence: ảnh UI thật, Jan/empty PDF/XLSX synthetic, previews và compact JSON checks. Không commit raw HTTP responses, JWT/auth headers, credential môi trường thật, database dump, heavy logs hoặc temporary helper/clone trong work/. Login screenshots chỉ có fixture email/credential demo công khai vốn đã có trong source/UI; input password bị mask, không secret môi trường.

[Validation summary](evidence/validation-final.json), [demo rehearsal](DEMO_REHEARSAL_REPORT.md), [HTTP evidence](evidence/http-regression.json), [GD6 smoke](evidence/smoke-gd6.json), [security patch](SECURITY_PATCH.md), [known issues](KNOWN_ISSUES.md).

## 5. Bàn giao và nội dung báo cáo

Người nhận dùng README/setup script, chọn đúng revision DEV cung cấp, mở hai terminal API/Web, login Development fixtures rồi theo DEMO_SCRIPT. Dừng bằng Ctrl+C server của mình; RAM restart resets demo, không reset Git/database thực. SQL persistent demo dùng dedicated database và process env như runbook.

[Nội dung báo cáo công khai](GD6_REPORT_CONTENT.md) và [tổng kết](PROJECT_FINAL_SUMMARY.md) sẵn để biên tập báo cáo thực tập; bản định danh cá nhân ngoài Git. Không chỉnh Word đã tìm thấy, không giả nhật ký tương lai/chữ ký/mentor feedback. Ownership ghi theo vai trò và evidence, không nhận phần BA/Tester là DEV.

Sẵn sàng **chuẩn bị báo cáo và review local MVP có điều kiện**, chưa nghiệm thu BA/mentor hoặc production. Mentor cần chốt scenario, BA semantics, runtime patch/deployment và các live integrations trước sign-off.
