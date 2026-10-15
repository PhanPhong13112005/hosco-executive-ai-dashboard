# Chỉ mục quyết định kiến trúc
Các ADR dưới đây là ghi nhận hồi cứu từ source/docs, không giả biên bản duyệt. Owner kỹ thuật: DEV. Approval business/deployment: PENDING mentor/BA.
| ADR | Quyết định thực tế | Lý do / trade-off | Trace |
|---|---|---|---|
| ADR01 | .NET10, EF Core, React/TypeScript/Vite | SDK10.0.400 đã chọn trong global.json; layers phân tách kiểm thử. Phụ thuộc SDK/vendor patches | README, csproj |
| ADR02 | SQL Server persistent; SQLite RAM test | Relational constraints trong offline tests, dễ reset; không thay thế SQL Server migration/concurrency validation | Program.InitializeDatabaseAsync |
| ADR03 | Canonical reporting/query catalog | Một nguồn KPI dùng Dashboard/Alert/Chat; không nhân bản công thức | Application/Semantics, KpiSemantics |
| ADR04 | Chat qua Reporting HTTP API | Giữ auth/scope boundary và audit, không cho LLM SQL tùy ý; thêm HTTP hop/timeout | ReportingApiClient, ChatGuards |
| ADR05 | Mock/fake provider trong automated tests | Không phí/credential/network flaky, đo được no-call/authorization; không chứng minh live model | LlmProviderTests, LlmChatApiTests |
| ADR06 | API guard độc lập model | Output JSON vẫn không đáng tin; validate enum/date/limit/scope và fixed endpoints | LlmIntentContract, ChatAuthorizationGuard |
| ADR07 | Persistent alert/delivery idempotency | Dedup/cooldown/escalation/retry được lưu; scheduler nhiều instance cần deployment review | AlertEngine, NotificationDeliveryStore |
| ADR08 | Bounded retry + fallback | Không treo vô hạn, không fabricate numbers; Reporting8s, chat route30s, provider total budget5s/retry1 mặc định | LLM_PROVIDER, middleware |
| ADR09 | Embedded Unicode font | PDF không lệ thuộc font host; thêm PDFsharp/font và license cần bàn giao | DashboardPdfWriter, Assets/Fonts |
| ADR10 | Crypto patch10.0.13 | Remediate October13 advisories, không suppress NuGetAudit; không thay KPI | SECURITY_PATCH.md |
ADR cần mở rộng khi có BA default-date/context decision, multi-instance scheduling hoặc production privacy approval.
