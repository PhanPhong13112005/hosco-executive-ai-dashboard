# GD5 — bàn giao phiên bản sửa lỗi

Ngày: 2026-10-10 (Asia/Saigon). Phạm vi: DEV local Mock/SQLite, không phải nghiệm thu production.

## Revision và ranh giới

- Chỉ branch `fix/gd5-tester-findings`; baseline `d2c4a7801840b96718abd1b3f7cf9ca4e230aebe`.
- Ba commit local, theo thứ tự: `fix: address gd5 self-qa export scope and ui defects`; `test: add gd5 self-qa regression coverage`; `docs: prepare gd5 self-qa handoff evidence`.
- Không push/merge, không thay branch, không thay main và không xử lý local main/origin/main. Không reset/restore/rebase/cherry-pick.
- Final commit hashes được lấy bằng `git log -3`, không chèn hash tự tham chiếu của docs commit vào chính file này.

## Review 5 lỗi và traceability

| Lỗi | File sửa | Regression / retest |
|---|---|---|
| PDF mất dấu/cắt nội dung | DashboardExportService.cs, DashboardPdfWriter.cs, Infrastructure.csproj, font Noto Sans | `Pdf_embeds_Vietnamese_font_wraps_full_text_and_keeps_rows_after_old_48_line_limit`: 140 rows, long text, Unicode CMap, embedded FontFile2, populated multiple pages; concurrent 8 exports. Actual download text/last inventory row + render được kiểm tra riêng. Test không tự chứng minh mọi prefix của 140 rows được trích xuất. |
| Excel cắt chữ | DashboardExportService.cs | `Empty_export_represents_null_ratios_as_NA_and_preserves_real_zero_as_numeric`: widths/wrap/N-A/typed zero. Actual January workbook import/render A1:F16, đủ KPI label/date/branch; content checks. |
| Alert Rule Save HTTP400 | AlertModels.cs | Theory Medium/High/Critical; unknown string400/no mutation; config preserved. HTTP Owner200, BranchManager403, tenant IDOR404. Evidence UI Save không error. |
| Chat bỏ scope Hà Nội | ChatIntentResolver.cs | 5 Unit variants; integration `Unprefixed_city_in_chat_is_not_silently_dropped_and_is_authorized`; HTTP Owner403/Chain200, data bằng direct Hà Nội API. |
| Logout ẩn màn hình hẹp | styles.css | `mobile sidebar never hides the only logout control`; ảnh mobile button visible + Self-QA click logout về login. Static CSS test không thay thế UI retest trước đó. |

Screenshot UI là evidence của đợt Self-QA trước bàn giao, không giả định đã chạy lại toàn bộ browser actions trong lượt bàn giao này. Handoff đã chạy lại toàn bộ automated suites, HTTP regression và export content/render.

## Final validation đã chạy lại

| Gate | PASS | FAIL / skip |
|---|---|---|
| Backend build --no-restore | 0 errors / 0 warnings | 0 |
| Unit --no-build | 180/180 | 0 / 0 |
| Integration --no-build | 164/164 | 0 / 0 |
| Frontend TypeScript/Vite build | PASS | 0 |
| Frontend Node regression | 4/4 | 0 / 0 |
| HTTP qua frontend5173 → backend5000 | 167/167 assertions, 146 requests | 0; fresh demo DB, AL04 mutation không skip |
| PDF/XLSX content | 15/15 checks + render/view | 0 |
| Git diff --check (working + staged mỗi nhóm) | PASS | 0 |

Tổng **348 automated tests PASS** (344 backend + 4 frontend), **0 FAIL, 0 skipped**. Thêm **182 HTTP/export assertions PASS**, 0 FAIL; các loại kiểm tra có coverage chồng lấn, không phải 530 AI test cases độc lập.

[Validation output](self-qa/evidence/validation-handoff.log), [HTTP compact evidence](self-qa/evidence/http-handoff-summary.json), [export checks](self-qa/evidence/export-inspection.json), [Self-QA chi tiết](GD5_SELF_QA_REPORT.md).

AI dataset: 28 rows observed; 26 current MVP intent/status/operation/safety contract checks PASS. AI005/038 giữ `BA_PENDING_ORACLE`, không ép thành PASS. 13 positive intents so với direct canonical API. BUG-PAGE-01, BUG-EMPTY-01, AI019/020 covered trong fresh HTTP suite. Không duyệt lại công thức KPI.

## Code / secret / scope review

- Đọc toàn bộ tracked diff và các untracked source/tests/tools/docs; kiểm kê mọi untracked evidence.
- Application không chứa QA expected numbers, seed IDs hoặc special-case test responses để ép PASS. Fixtures/expected values chỉ trong tests/QA runner, đối chiếu đúng DemoSeed và canonical API.
- Chat chỉ bổ sung city reference parsing; authorization tiếp tục dùng branch directory theo JWT. Export vẫn lấy dữ liệu từ scoped reporting/alert repositories. Negative tenant/branch/RBAC tests vẫn PASS.
- Không sửa business calculators/KPI semantics, migrations/seed, recipients/security policies, production appsettings, launch config hay deployment config.
- Không API key, JWT/signing key, bot token, private key hoặc password thực trong staged changes/evidence. New C# regression tham chiếu public Development `DemoSeed.DemoPassword`; Python tools lấy credential demo qua process env `HOSCO_QA_DEMO_PASSWORD`, không ghi giá trị. Negative-login test dùng chuỗi sai giả lập. Không dùng credential production.
- Reviewed logs chỉ chứa command/results và synthetic demo data; HTTP handoff summary bỏ toàn bộ request bodies/full response payloads/auth headers. Các ảnh được xem lại không chứa credential/token thật. Ảnh chat partial không được coi là bằng chứng đầy đủ refusal AI020.
- PDFsharp6.2.2 pinned; NotoSans binary là runtime resource cần thiết, không phải log lớn. SHA256 và OFL license trong Assets/Fonts/README.md/LICENSE.txt. Không phụ thuộc font Windows/network runtime.
- Không disable/bypass WDAC hoặc reinstall .NET. Trusted local validation/runtime UNBLOCKED; không chứng minh Enterprise WDAC được gỡ ở mọi execution context.

## File inventory và artifact preservation

[Inventory đầy đủ](self-qa/evidence/handoff-file-inventory.json) liệt kê từng file sửa/thêm theo 3 nhóm cùng SHA256 trước commit. Manifest không tự hash chính nó. Font/sources/tests là implementation thực, không bị bỏ sót.

Nhóm fixes (9 files):

- src/Hosco.Application/Models/AlertModels.cs
- src/Hosco.Application/Services/ChatIntentResolver.cs
- src/Hosco.Infrastructure/Hosco.Infrastructure.csproj
- src/Hosco.Infrastructure/Persistence/DashboardExportService.cs
- src/Hosco.Infrastructure/Persistence/DashboardPdfWriter.cs
- src/Hosco.Infrastructure/Assets/Fonts/NotoSans-Regular.ttf
- src/Hosco.Infrastructure/Assets/Fonts/LICENSE.txt
- src/Hosco.Infrastructure/Assets/Fonts/README.md
- src/Hosco.Web/src/styles.css

Nhóm regression (5 files, gồm 2 runner HTTP/export):

- tests/Hosco.UnitTests/Gd5IntentRegressionTests.cs
- tests/Hosco.IntegrationTests/Gd5SelfQaRegressionTests.cs
- tests/Hosco.WebTests/mobile-logout.test.mjs
- docs/testing/self-qa/Verify-Gd5SelfQa.py
- docs/testing/self-qa/Inspect-Exports.py

Nhóm docs gồm GD5_DEV_TRIAGE_REPORT.md (đính chính API null), GD5_SELF_QA_REPORT.md, report này, evidence nhỏ đã rà soát và .gitattributes chỉ cho PDF evidence.

Kiểm tra staged bắt được Git coi PDF demo ASCII trước sửa là text: xref có khoảng trắng bắt buộc và autocrlf có thể đổi byte offsets. Quy tắc binary scoped cho PDF evidence bảo toàn nguyên byte, không rewrite PDF hoặc bỏ kiểm tra whitespace cho source. Staged diff check được chạy lại sau khi áp dụng; mọi source/tests/docs text vẫn được kiểm tra bình thường.

**12 untracked artifacts không commit**: ảnh trùng, raw HTTP replay lớn và logs/bản render trung gian được chuyển vào ignored `work/gd5/handoff-archive`. [Danh sách + hash](self-qa/evidence/handoff-archive-inventory.json). Mỗi file giữ nguyên SHA256 trước/sau move, recoverable tại đường dẫn local đó; không xóa data/source. Không commit bin/obj/dist/node_modules, render helper, credentials hay heavy raw logs. Stage từng file bằng tên cụ thể, không `git add .`/`git add -A`.

7 original Tester files đã kiểm tra lại SHA256, tất cả unchanged. Database QA là SQLite RAM của tiến trình tester, không phải database production. Backend/frontend do lượt bàn giao khởi động đã dừng bằng Ctrl+C; exit1 là intentional interrupt, không phải validation failure.

## Chưa được kiểm chứng / vẫn pending

- Nghiệm thu của Tester trên môi trường gốc, đặc biệt BUG-UI-001; BA formula/oracle decisions vẫn PENDING.
- Live OpenAI/Gemini và real Telegram delivery/cost/network: không có credentials thật; chỉ fake/mock tests.
- SQL Server deployment, load/performance SLA, production configs/certificate/signing/WDAC policy tại máy khác.
- Native Microsoft Excel behavior và mọi viewport/browser chưa test; workbook được read/import/render, không mở bằng Excel native.
- JWT time-based expiry riêng không được suy diễn từ retest token invalid sau process key rotation.

**Kết luận: phiên bản sửa 5 lỗi đã qua DEV final validation và sẵn sàng bàn giao để Tester retest. Không tuyên bố đã nghiệm thu production.**
