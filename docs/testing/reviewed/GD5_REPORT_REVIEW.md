# GD5 — Review báo cáo Security / Performance / Traceability

Đây là bản review mới, không ghi đè 7 tài liệu Tester. Original hashes: [audit JSON](evidence/gd5-traceability-audit.json). DEV đọc text/tables toàn bộ và toàn bộ 105 dataset rows; không coi ảnh nhúng trong DOCX là raw request logs đã xác minh. Ảnh UI mới do DEV chụp riêng tại local.

## Security: review từng hàng của ma trận 8/8

“8/8” là 8 hàng, trong đó DML và AI gom nhiều case. ID hai chữ số của báo cáo cần map rõ sang ID ba chữ số trong RTM; không tự coi chúng là cùng một lần execution.

| Hàng Tester | Evidence DEV thực tế | Kết luận / giới hạn |
|---|---|---|
| TC-AUTH-01 | Fresh runtime Reporting không Bearer → 401; ReportingApiTests.Reporting_without_login_returns_401, ChatApiTests.Chat_requires_authentication đã chạy. | Behavior xác nhận. Không chứng minh toàn bộ JWT crypto attack matrix. |
| TC-TENANT-01 | ReportingApiTests.Cross_tenant_branch_is_forbidden, Reporting_never_leaks_other_tenant_data, Client_tenantId_query_parameter_cannot_override_claim; ranking owner@fixture chỉ SKU tenant B. | Explicit scoped LINQ + claims + ReportingScopeFactory/BranchScopeValidator, **không HasQueryFilter/Global Query Filter**. IDOR alerts có Alert_detail_enforces_tenant_and_branch_isolation. Không khẳng định mọi entity tự lọc toàn cục. |
| TC-SCOPE-01 | Branch_manager_can_read_assigned_branch_only / is_forbidden_from_other_assigned_tenant_branch; Owner/SystemAdmin cũng không có quyền chi nhánh chưa được gán; natural alias Owner 403 / ChainManager 200. | Branch assignment thật, ChainManager chỉ tenant mình. Không đồng nhất Owner/Admin với tenant-wide hay cross-tenant. |
| TC-DML-01..03 | Fresh runtime POST/PUT/DELETE /reporting/orders → 405; ReportingController là GET, reads dùng AsNoTracking. | Read-only HTTP surface xác nhận, **không chứng minh DB account SELECT-only/db_datareader**. App startup migrate/seed có writes; cấu hình không cho thấy dedicated reporting read-only account. |
| TC-DATA-01 | Public reporting DTO source không expose password/secret; raw response chỉ public fields, existing API/LLM tests truyền số liệu chuẩn. | Source inspection + sampled runtime, không scan mọi DTO/production logs. Auth login được phép trả accessToken; không gán “mọi API không có token”. Telegram fake HTTP tests kiểm tra channel không log credentials. |
| TC-DATA-02 | GUID sai/SQLi GUID →400 validation_error không stack; middleware Testing/Production unexpected500 generic. | Source dùng ApiError{code,message,correlationId}, **không RFC7807 ProblemDetails**. Development non-chat có thể trả exception.Message; malformed GUID không thực thi unexpected500 nên không chứng minh mọi ngoại lệ/SQL secret được che. Production hardening/TLS/log access NOT VERIFIED. |
| TC-SQLI-01 | Fresh runtime BranchId=' OR '1'='1 →400 ở model binding; source report query LINQ scoped, không tìm thấy dynamic SQL executor. | Payload này xác nhận strong type rejection, không full SQLi penetration test hay bằng chứng SQL Server sp_executesql trace. Parameter thực tế From/To, không fromDate như báo cáo. |
| TC-AI-01..03 | 019/020 baseline Revenue Ambiguous không data/query; sau fix403. Mock và fake LLM tests khẳng định provider/reporting calls không tăng với unsafe input kể cả có ngày. Schema/direct DB/password variants403;9 Mock smoke pass. | Structured JSON intent + enum/API/query allowlist và reauthorization, **không arbitrary Function/Tool Calling executor, không Text-to-SQL**. Baseline refusal gap không chứng minh leak. Lexical guard + test sample không bảo đảm chống mọi jailbreak. Live GPT/Gemini NOT CONFIGURED. |

Không có cơ sở lặp lại câu “100% rủi ro được triệt tiêu / không có bất kỳ lỗ hổng nào”. Các kết quả trên chỉ áp dụng phạm vi source/test/runtime đã chạy. TLS, encryption-at-rest, deployment DB roles, full pentest, secret-log audit và live provider cần evidence riêng.

## Performance / reliability

Báo cáo có Passed đồng thời chi tiết “chưa thực thi”; dấu P95 >=3s sai chiều SRS <=3s. Vài ảnh 3.13–10.4s chỉ là sample request, không đủ tính P95 chính thức. Không quy lỗi DB/EF/network/WDAC nếu thiếu trace.

DEV đo local Windows 11 build26200, Ryzen5 7535HS (6 cores/12 threads), RAM16GB nominal (15.32GiB visible), .NET10.0.400 / runtime10.0.11. API Testing + Mock, fresh SQLite shared-memory DemoSeed A-HCM tháng01/2026; không dùng SQL Server HoscoTester. Năm warm-up/endpoint; bốn endpoints; 100 requests/endpoint cho concurrency1 và4 (tổng800). Error rate 0/800. P95 nearest-rank `sorted[ceil(.95*N)-1]`, tất cả sample giữ lại.

| Endpoint | P95 c=1 (ms) | P95 c=4 (ms) | N mỗi c |
|---|---:|---:|---:|
| /reporting/dashboard/summary | 40.279 | 35.639 | 100 |
| /reporting/revenue/trend | 41.615 | 35.913 | 100 |
| /reporting/products/top | 34.217 | 37.064 | 100 |
| /reporting/products/bottom | 38.047 | 28.308 | 100 |

Raw: [CSV 800 samples](evidence/gd5-benchmark-raw.csv); method/environment: [final runtime JSON](evidence/gd5-final-runtime.json). Đây là **LOCAL BENCHMARK ONLY**, không nghiệm thu SLA SQL Server/Tester's DB; Chat P95 chưa đo. Không thấy bottleneck trên workload này nên không tối ưu speculative. Cần retest đúng DB, data volume, cold/warm, concurrency/network và correlation traces để điều tra các sample Tester chậm.

Stack thực tế:

- ASP.NET request default10s, chat policy30s; SQLServer command timeout10s + EnableRetryOnFailure(3). Đây không tự đảm bảo end-to-end <=10s khi có nhiều layers/retries.
- ReportingHttpClient configured8s; không Reporting HTTP retry/cache fallback. Shared SQLite connection Cache=Shared không phải cached report.
- LLM timeout budget default5s (config tối đa10s), MaxRetries default1/clamp0..1; deterministic fallback vẫn gọi actual scoped Reporting API, không tạo data khi API unavailable.
- Telegram channel default timeout10s (clamp1..30), retries3 (1+retries attempts), exponential delay base500ms. EF-persisted NotificationDelivery + NotificationDispatcher.RetryPendingAsync + scheduler, idempotency; **không BullMQ**.
- Existing fake HTTP/unit/integration tests kiểm tra Telegram timeout/5xx/disabled/scoped recipient, dispatch retry/idempotency/persistence. Không Telegram live/token thật, không production fault injection. SRS retry budget / policy chưa nghiệm thu end-to-end.

## Traceability / test-plan review

Actual workbook audit:14 unique FR/NFR (FR01–08/NFR01–06),30 RTM rows,116 unique case IDs,0 duplicate,0 linked-ID missing,0 case unlinked.21 test-data groups,19 **designed** fixtures,21 API rows,8 KPI mappings,16 AC rows,19 BA questions. Tất cả116 execution status vẫn **NOT RUN**; không phải116 PASS, không suy ra19 fixtures đã insert DB. Dataset105 IDs cũng unique; original labels30Pass/66Fail/9Blocked đúng thống kê nhưng không phải verified accuracy.

21 API paths/methods trong sheet06 khớp current Swagger/controller. Kiểm tra mapping KPI source và ruleAL01–05: Revenue/GMV/Orders/AOV/GrossProfit/GrossMargin/CancelReturn/DangerousStock có API; SKU ranking quantity là KPI07 / TopBottom phụ trợ. Formula eligibility và time/rounding còn BA Pending, không chỉ dựa nhãn source. Alert numeric thresholds là BA-proposed configurable, không gán AL02=30 immutable; recipient/RBAC thực tế nằm policy, AL04 action BranchManager-only.

Những expected cần sửa ở bản reviewed trước execution:

| Item | Mâu thuẫn | Retest/correction |
|---|---|---|
| TC-API-016 | Yêu cầu pagination/sort kpis/summary là danh sách static8metrics. | Dùng orders/ranking cho pagination, không thêm pages cho8KPIs. |
| TC-API-017..019 | Shared filter validation Page>=1/PageSize1..200/SortDirectionasc-desc không có nghĩa tất cả endpoints thực thi sort/pagination. | Kiểm tra validation và khả năng từng route riêng; summary không paginated. |
| TC-DASH-007 | Max10 đúng dashboard gọi PageSize10, nhưng direct ranking default50/max200. | Ghi rõ dashboard UI hay direct API. |
| TC-DASH-003 | /orders chứa mọi status; totalCount hay số dòng1page không phải canonical TotalOrders93/87. | Đối chiếu dashboard/summary hoặc kpis/summary + SQL cùng eligibility đã được BA chốt. |
| TC-DASH-016/017 | Empty AOV/Margin hiện null/NA; Tester0 không tái hiện current build. | Giữ API null, UI N/A và valid0. Source unchanged, thêm regression. |
| TC-DASH-009/020/021/022 | Stock historical/current, timestamps và rounding chưa có oracle duyệt rõ. | [BA decisions](../BA_DECISIONS.md), không sửa source theo giả định. |
| Query identifiers | QueryCatalog có revenue.summary.v1 nhưng /revenue hiện response meta revenue.trend.v1; /kpis/summary meta kpis.summary.v1, không mọi per-metric catalog ID đều là route executed. | Ghi actual response queryId trong evidence, không dùng semantic catalog existence để kết luận route đã execute. |
| AI005/007/025/033/068/071/073/074/085 | Status Pass không khớp expected/missing dimension. | Giữ original; bảng105 ghi rõ oracle mismatch hoặc dimension gap, không sửa nhãn cũ thành verified Pass. |

Không ghi đè XLSX/DOCX. Bản reviewed này và AI triage là delta review, không phải RTM mới đã được BA duyệt hay execution của toàn116 case.
