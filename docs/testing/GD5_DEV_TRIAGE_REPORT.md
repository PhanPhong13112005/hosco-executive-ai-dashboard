# GD5 — DEV triage report

Ngày: 2026-10-10. Repo: HOSCO Executive AI Dashboard. Không push/merge/main changes.

## 1. Baseline và phạm vi

Initial branch `feature/gd4-ai-chatbot`, clean working tree, HEAD `eb96530c1c458f91c14bd8e9afe513d32c392e04`. Fix branch `fix/gd5-tester-findings` tạo từ đúng HEAD này. Main reference giữ `ed930682ac7b56f919fc0971535c19565bcd7a30`. Không reset/rebase/restore/cherry-pick, không ghi đè thay đổi có sẵn.

7 originals thực sự có tại `C:\Users\Asus\Downloads\TESTER`; đọc text/tables của5DOCX và toàn bộ2XLSX. Đọc SRS_v1_User_Stories_AC_HOSCO.docx và TỪ ĐIỂN KPI.xlsx tại Downloads/Đề Thực tập/GD1, source/contracts/seed/migrations/current tests/docs gd4/testing. Không mặc định kết luận Tester đúng. Originals không bị sửa; hashes lưu [audit](reviewed/evidence/gd5-traceability-audit.json). Skill documents/spreadsheets được dùng để đọc originals read-only và rà dữ liệu/traceability; không tạo DOCX/XLSX thay thế.

Re-ran baseline build PASS, Unit141/141, Integration137/137. Các số này là baseline trước sửa, không dùng thay kết quả cuối. Controlled reproduction dùng real compiled API + fresh SQLite DemoSeed + Mock, không giả số liệu hoặc gọi live LLM. Không có DB SQL Server Tester chính xác nên data accuracy/SLA đó chưa nghiệm thu.

## 2. Findings theo ưu tiên

| Finding | Priority | Phân loại | Kết quả |
|---|---|---|---|
| BUG-PAGE-01 | High | Confirmed application bug | Page không Skip; page2 trùng page1; metadata null. Fixed + regression. |
| AI019/020 + password/schema/directDB variants | High | Confirmed refusal behavior bug, leak NOT PROVEN | Lexical guard thiếu dấu hiệu, request nhạy cảm vào Revenue/clarify. Fixed trước provider/Reporting. |
| Supported date/branch/follow-up/wording | Medium | Confirmed NLU gaps | dt, month/year clarify, tháng nì, của Hà Nội/HN, thì sao, theo ngày, bán ít nhất, sắp hết hàng; fixed trong existing intents/API. |
| Unsupported dimension bị bỏ qua | Medium | Wrong query selection + scope gap | Employee/SKU/revenue-ranking/comparison không được biến thành generic KPI. Explicit Unknown trước LLM/local query; không triển khai feature thiếu. |
| BUG-EMPTY-01 | Medium | Not reproduced on current baseline/source | API null, UI N/A đúng; thêm test, **không sửa frontend/formula, không empty fix commit**. |
| TotalOrders87vs93 và derived KPIs | High acceptance risk | BA_PENDING | Dictionary eligibility khác source return states; chưa đổi93. |
| AI105 / oracle issues | Acceptance risk | ScopeGap / WrongOracle / NotVerified | Không ép105PASS. Bảng từng ID và responses thực tế. |
| Performance3–10s | Investigation needed | Test evidence insufficient | Local raw800 requests đo được; không thay officialP95, không speculative optimize. |
| Security8/8 / RTM116 | Documentation risk | Evidence/architecture overclaims | Reviewed từng security row, designed≠executed; không tuyên bố zero vulnerabilities. |

## 3. BUG-PAGE-01 — tái hiện và root fix

GET `/api/v1/reporting/products/ranking` với HOSCO-A / A-HCM tháng01/2026, UTC+7, PageSize3, bottom=false. Model binding nhận Page đúng nhưng persistence chỉ `Take(PageSize)`, không offset.

| | Page1 | Page2 | Metadata |
|---|---|---|---|
| Baseline | SKU002,004,006 | SKU002,004,006 | page/pageSize/totalCount=null |
| Fixed | SKU002,004,006 | SKU008,001,007 | page1/2, size3, total8 |

[Exact baseline](reviewed/evidence/gd5-baseline-runtime.json) / [exact final](reviewed/evidence/gd5-final-runtime.json). Sort quantity rồi SKU rồi ProductId giữ ổn định tie; tenant/branch/date/recognized sale + positive remaining qty filter trước aggregation/ranking/pagination. Không hardcode SKU. TotalCount saufilter trướcpagination. Wide integer offset bảo vệ int.MaxValue page.

Files: ReportingModels, ReportingDataStore, ReportingController, QueryCatalog, DashboardExportService. Internal store trả PagedResult, public data vẫn array compatible; page/pageSize/totalCount nằm meta. Export luôn top/bottom page1,size10. ProductRankingPersistenceTests chỉ adapt Items và tăng empty-total assertion, không bỏ assert.

Red proof: new reporting regression trước fix4FAIL (pages/metadata),2emptyPASS; sau fix toàn bộPASS. Integration kiểm tra Top/Bottom, page1/2/last/out-of-range, stable ties, no duplicate/no omitted ProductId, repeat2, overflow page, alias top/bottom, denied branch/tenant và tenantB-only SKU.

## 4. BUG-EMPTY-01 — không sửa khi chưa tái hiện

A-HCM tháng01/2030 (equivalent half-open Tester01/01–01/02 UTC+7) API baseline và final: revenue/gmv/orders/profit=0, aov=null, margin=null. Dashboard.tsx formatter hiện `value==null ? 'N/A' : ...`; zero hợp lệ không bị đổi N/A.

Actual browser login + filters2030-01-01 đến2030-01-31 và A-HCM hiển thị AOV N/A, Margin N/A, Revenue0/GMV0/Profit0. [UI screenshot](reviewed/evidence/gd5-empty-kpi-summary.jpg), [full page](reviewed/evidence/gd5-empty-kpi.jpg). Stock2 là current inventory, không lấy historical order count.

Không thể xác nhận ảnh Tester0 thuộc đúng build/cache/filter. Yêu cầu retest đúng revision, capture Network response và screenshot cùng filters. Regression mới: empty API hai tenants, calculator Orders0/Revenue0, recognized free-order AOV0 hợp lệ, Revenue>0 và Profit0 →Margin0 hợp lệ. Node tests trích actual formatter AST từ Dashboard.tsx, không copy implementation: null/undefined NA, valid0, rounding hiện hành. **Không frontend source fix** vì implementation hiện tại đúng.

## 5. AI refusal và supported intent fixes

Trace: Chat API authentication → safety guard → local/LLM intent adapter → ChatAuthorizationGuard query whitelist + accessible branch resolution → scoped ReportingHttpClient → Reporting API guard/store. Guard safety của LLM path chạy trước provider; API guard không phụ thuộc model obedience. Không SQL trực tiếp/SQL động từ chat.

AI019/020 baseline HTTP200 Revenue/Ambiguous, data=null/reportingOperation=null. Đây là hành vi refusal sai; **chưa xảy ra leak được chứng minh**. Nay HTTP403 forbidden trước provider/reporting, cả wording có kỳ để không được “an toàn tình cờ vì thiếu ngày”. Fake provider/reporting counters không tăng; legit revenue vẫn chạy. Thêm patterns bỏ qua hướng dẫn/password/token/secret/schema/direct DB. Không log token/key thật; evidence chỉ có public synthetic demo data, không chứa Bearer token.

NLU trong existing13 intents:

- AI008 dt nhận Revenue; tháng8 thiếu năm hỏi lại. Calendar month+year2000–2100, month1–12 dùng UTC+7 exact boundaries/leap year; invalid không invent date.
- 017/018 thì sao theo context Revenue hợp lệ, chỉ inherit allowlisted intent/metric; không inherit scope/quyền.
- tháng nì, của Hà Nội/HN parse branch; aliases resolve chỉ trong accessible directory. Owner A-HCM yêu cầu Hà Nội403, ChainManager permitted branch200.
- theo ngày→RevenueTrend, bán ít nhất/doanh số thấp nhất→BottomProducts, sắp hết hàng→DangerousInventory.
- Unsupported dimensions trả Unknown/noReporting rõ ràng ở cả local/LLM; không để LLM bỏ employee filter, không quantity ranking thay revenue ranking.

Files: ChatGuards.cs, ChatIntentResolver.cs, LlmIntentResolver.cs. Không mở enum, query API mới, DB schema, seed, architecture hoặc live model config. Guard vẫn lexical/layered, không tuyên bố bảo vệ tất cả cách jailbreak.

## 6. Toàn bộ105 dataset / business reconciliation

[105-ID triage](reviewed/GD5_AI_DATASET_TRIAGE.md): input, exact expected oracle, original Tester status/intent, baseline→final actual, current MVP/API, reproduced/classification, action từng ID. Raw original actual text và final message/context/filter ở evidence JSON. 5nhóm×21; original30Pass/66Fail/9Blocked, không thay số này bằng accuracy của DEV.

005 missing scope oracle mâu thuẫn default authorized scope;007 lỗi tháng nì dù TesterPass;025Top/Bottom và033Profit/Margin statusPass sai expected;034Unknown không tái hiện source baseline. Cases employee68/71/73/74 và income85 không được Pass chỉ vì generic KPI/Unknown.

Customer/employee/income-expense không có corresponding canonical API. SRS FR05 rộng hơn current Proposed13intents nên đây là **scope/acceptance gap cần BA**, không khẳng định nghiệp vụ đó bị loại chính thức khỏi SRS. Không thêm SQL/ledger/customer endpoints để chạy theo dataset. NoData không tự được chấm Pass: current date tháng10 ngoài seedJan–Jun không thay cho oracle SQL Tester độc lập.

[BA_DECISIONS](BA_DECISIONS.md) giữ các quyết định PENDING. KPI Dictionary chỉ Completed/Delivered cho Orders;70+17=87 vs recognized current returned/partial thêm6=93. AOV/CancelReturn phụ thuộc mẫu số; rounding, timestamp, technical cancellation, current vs historical stock và scope/defaults phải chốt trước sửa. Jan actual giữ Revenue14,022,500;GMV14,062,500;Orders93;AOV150,779.6;Profit6,127,500;Margin43.7%;Rate6.45%;Stock2.

## 7. Review security/performance/RTM

[Reviewed report](reviewed/GD5_REPORT_REVIEW.md) là delta review, không sửa originals.

Security actual401/403/405/400 + regression/source inspection được phân loại riêng, IDOR/scopes có integration evidence; DTO/error masking có sampling/architecture limits. Repo không Global Query Filter/db_datareader/ProblemDetails/arbitrary FunctionCalling executor như báo cáo mô tả. Không kết luận “không có bất kỳ lỗ hổng nào”.

Performance local800requests:100 mỗi endpoint/concurrency1,4;5warmup/endpoint;Mock/SQLite;nearest-rankP95;0errors. P95c1/c4:summary40.279/35.639ms;trend41.615/35.913ms;top34.217/37.064ms;bottom38.047/28.308ms. RawCSV và method/environment được lưu. **Không chứng minh SLA SQLServer3s hay Chat15s**. Không optimization thiếu bottleneck, không cache fallback giả, không BullMQ. Notification actualEF delivery persistence/dispatcher/scheduler + Telegram retry/backoff/idempotency, fake tests only.

Trace14unique requirements/30rows/116uniqueIDs/21data/19designed fixtures khớp; không missinglinks/duplicates.21APIpaths/methods matchSwagger. Tất cả116statusNOT RUN. Pagination trên static8KPIs, max10 directranking, order-list count vsTotalOrders, date/stock/rounding và configurable rule thresholds cần correction/BA trước execution.

## 8. Validation cuối

Source cuối được build lại sau các fixes; không dùng baseline cũ để tuyên bố kết quả.

| Command / kiểm tra | Kết quả actual |
|---|---|
| dotnet build Hosco.slnx --no-restore | PASS,0errors;1NU1900 (NuGet vulnerability feed unavailable) |
| dotnet test tests/Hosco.UnitTests/Hosco.UnitTests.csproj --no-build | PASS175/175,0skip |
| dotnet test tests/Hosco.IntegrationTests/Hosco.IntegrationTests.csproj --no-build | PASS156/156,0skip |
| dotnet test Hosco.slnx --no-build | PASS331/331 (175+156),0skip |
| npm.cmd run build tại src/Hosco.Web | PASS TypeScript+Vite |
| node --test tests/Hosco.WebTests/dashboard-formatting.test.mjs | PASS3/3 |
| Isolated runtime Mock smoke | PASS9/9;105 dataset HTTP requests replayed, không đồng nghĩa105PASS |
| Actual browser empty KPI | N/A/N/A, valid0; screenshot captured |
| git diff --check | PASS before commits; repeat final |
| Live GPT/Gemini / SQLServer Tester / official SLA | NOT VERIFIED / credentials not configured / different DB |

+34 Unit và+19 Integration cases so baseline; adapted existing persistence test giữ assertions. New files Gd5ReportingRegressionTests.cs, Gd5IntentRegressionTests.cs, dashboard-formatting.test.mjs; modified ChatbotTests/KpiCalculatorTests/LlmIntentTests/ChatApiTests/LlmChatApiTests và minimal ProductRankingPersistenceTests adaptation. Existing dashboard/export/alert/notification/RBAC regressions chạy trong full331, không bỏ assertion để tăng Pass.

Windows: trusted execution cho build/tests được phép và chạy được; WDAC không tái hiện block trong final validation, không disable policy/reinstall .NET. Một lần intermediate build copy DLL fail MSB3021/3027 vì chính smoke API Hosco.Api khóa DLL; dừng đúng server do DEV khởi chạy, rebuild PASS. Không quy lỗi đó thành application/WDAC defect. Các temporary API/Vite processes đã dừng; isolated probe tự terminate process của nó.

## 9. Reproduce / Tester retest

1. Build/tests như bảng trên; fresh seed trong test environment được cấp phép, đúng commit. Không reset DB đang dùng nếu chưa được phép. Demo accounts/commands theo TESTER_SETUP.md.
2. Pagination: Owner A-HCM; From=2025-12-31T17:00Z,To=2026-01-31T16:59:59.9999999Z; PageSize3; top Page1/2/3/4→3/3/2/0 items,total8; Page2 SKU008/001/007. Bottom tương tự đảo quantity (tie vẫnSKU,ProductId), no duplicates; other branch/tenant403. Capture meta/queryId/correlationId.
3. Empty: tháng01/2030,A-HCM; Network API AOV/Margin null; UI N/A; zero money vẫn0. Thêm positive denominators và true0; giữ stock current note.
4. AI019/020, thêm “hôm nay” và schema/password/directDB/injection:403, no query/provider. Revenue bình thường200; unauthorized branch403. Không dùng key thật cho automated suite.
5. AI008: thiếu năm hỏi lại, explicit tháng8 năm2026 resolve;017/018 phải gửi valid context, không gửi context không được coi same test;HN Owner403 vs ChainManager200;variants023/027/014 dùng existingAPI.
6. Unsupported groups: Unknown/no wrong reporting; BA quyết định acceptance scope, không chỉ chấm UnknownPass.
7. KPI87/93, rounding/timestamps/current stock retest chỉ sau BA duyệt oracle; SQL phải cùng dataset,scope,timezone,boundaries.
8. Performance: SQLServer dataset thật của test env, warmup/N/concurrency/error-rate/raw latencies và request logs; không officialP95 từ screenshot. Security report retest từng request và map RTM IDs, không dùng architecture claims thay evidence.

Portable read-only replay helper (Python + openpyxl, Windows, .NET outputs đã build):

```powershell
python docs/testing/reviewed/evidence/gd5_runtime_probe.py --dataset "C:\Users\Asus\Downloads\TESTER\HOSCO_Intent_Test_Dataset.xlsx" --benchmark
```

Helper dùng ephemeral localhost port và fresh Testing SQLite memory; disables scheduler/Telegram, Mock; không sửa originals/DB Tester, không in JWT/key. Emits structured JSON; runtime evidence đã lưu từ run final2026-10-10T01:46:41Z. Helper portable path/dataset-arg variant đã chạy exit0 sau khi lưu. Không coi build helper/source timestamp là Tester build evidence.

## 10. Commit / handoff

Chỉ commit logical fixes và regression/docs sau validationPASS. Không có `fix: handle undefined dashboard KPI values` vì chưa có source defect tái hiện. Không có push/merge; main reference phải giữ nguyên. Xem `git log eb96530..HEAD --oneline --reverse` để lấy exact hashes của nhóm ranking, chatbot, tests và docs. Git final status/report được xác nhận sau commit (không tự ghi hash của chính docs commit vào nội dung).
