# GD4 – AI Chatbot Implementation Plan

## Trạng thái yêu cầu

Repository hiện không có BA Conversation Design hoặc Intent Dataset. Tài liệu GD2 xác nhận các artefact này chưa được cung cấp và GD3 đánh dấu Chatbot là GD4/out-of-scope. Vì vậy các intent trong kế hoạch này là **PROPOSED MVP**, không được trình bày như requirement đã được BA phê duyệt.

## Mục tiêu

GD4 bổ sung trợ lý báo cáo chỉ đọc cho Executive Dashboard. Người dùng gửi câu hỏi tiếng Việt, backend phân giải intent và tham số, kiểm tra an toàn/quyền, ánh xạ sang operation được allowlist, gọi Reporting API bằng JWT hiện tại, rồi diễn đạt dữ liệu có cấu trúc mà không tính lại KPI.

> **CHATBOT DOES NOT ACCESS DATABASE DIRECTLY.**

Chatbot không tham chiếu EF Core, `HoscoDbContext`, repository persistence hoặc SQL. Chatbot không nhận URL từ người dùng, không sinh SQL và không cung cấp công cụ write/action.

## Luồng kiến trúc

```text
Hosco.Web Chat UI
    -> POST /api/v1/chat/messages (JWT)
    -> IChatService
       -> IChatSafetyGuard
       -> IIntentResolver
       -> IChatAuthorizationGuard / allowlist
       -> IReportingApiClient
          -> authenticated /api/v1/reporting/* or /api/v1/alerts
          -> existing ReportingScopeFactory / canonical KPI layer
          -> database
       -> IResponseComposer
    -> structured Chat response
```

`IReportingApiClient` là boundary bắt buộc. Adapter runtime dùng HTTP tới chính API host, forward bearer token và correlation ID, áp dụng timeout/cancellation, và chỉ xây URL từ enum operation nội bộ. Không có generic HTTP proxy.

## Proposed MVP intents

| Intent | Reporting operation | Trạng thái |
|---|---|---|
| KPI overview | Dashboard summary | PROPOSED |
| Revenue | Dashboard summary | PROPOSED |
| GMV | Dashboard summary | PROPOSED |
| Total orders | Dashboard summary | PROPOSED |
| AOV | Dashboard summary | PROPOSED |
| Gross profit | Dashboard summary | PROPOSED |
| Gross margin | Dashboard summary | PROPOSED |
| Cancellation/return rate | Dashboard summary | PROPOSED |
| Revenue trend | Revenue trend | PROPOSED |
| Top SKU | Product ranking (top) | PROPOSED |
| Bottom SKU | Product ranking (bottom) | PROPOSED |
| Dangerous stock | Dangerous inventory | PROPOSED |
| Current alerts | Alert list | PROPOSED |

Date phrases MVP: hôm nay, hôm qua, tuần này, tuần trước, tháng này, tháng trước và `N ngày gần nhất`, dùng `IBusinessTime` UTC+7 cùng `TimeProvider`.

Revenue/KPI questions thiếu time range thực sự mơ hồ sẽ yêu cầu làm rõ. Inventory và current alerts không bắt buộc date range. Follow-up đơn giản như “Còn tuần trước?” có thể reuse intent/metric từ context client, nhưng scope/branch luôn được resolve và authorize lại.

## Security design

- Chat endpoint bắt buộc policy `ReportingReader`.
- Không nhận `tenantId`, URL hoặc query ID tùy ý.
- Branch chỉ được resolve từ danh sách `/api/v1/reporting/branches` đã scope theo JWT; branch không nằm trong danh sách được phép trả 403 mà không xác nhận sự tồn tại.
- Guard từ chối yêu cầu bypass tenant/branch, prompt injection, đọc secret/system prompt, sinh SQL, connection string, command hoặc arbitrary URL.
- Mỗi operation được ánh xạ từ enum nội bộ và kiểm tra với Query Catalog/allowlist.
- Reporting client forward JWT/correlation nhưng không log token.
- Input có giới hạn độ dài; exception/stack trace/internal type không xuất hiện trong chat response.
- Audit ghi intent, operation, outcome và correlation qua hạ tầng hiện có; không ghi JWT/API key hoặc toàn bộ prompt nhạy cảm.

## Provider strategy

Core flow dùng deterministic resolver và không phụ thuộc dịch vụ trả phí. `ILlmProvider` là abstraction optional; implementation mặc định disabled trả no-result để deterministic resolver/fallback vẫn test và chạy offline. Không commit API key.

## Implementation phases và commit gates

1. `docs: add gd4 implementation plan`
   - Plan, status BA và architecture boundary.
2. `feat: add chatbot domain and application contracts`
   - Chat models, operation enum, service/provider interfaces.
3. `feat: implement chatbot intent resolution`
   - Vietnamese normalization, date extraction UTC+7, ambiguity/unknown/context.
4. `feat: add reporting api client and chat guard`
   - Safety guard, operation allowlist, branch resolution, authenticated typed HTTP adapter.
5. `feat: add chatbot api and response composer`
   - Orchestration, safe response composition, controller, DI, audit.
6. `feat: add chatbot web ui`
   - Enable sidebar, chat page/widget, loading/retry/empty/error/403, suggestions, minimal context.
7. `test: add gd4 chatbot unit coverage`
   - Intent/date, ambiguity, unknown, safety, guard, composer, provider fallback, scope behavior.
8. `test: add gd4 chatbot integration coverage`
   - Auth, 401/403, branch/tenant isolation, supported operations, failure/no-data and injection cases.
9. `docs: add gd4 implementation documentation`
   - Architecture, intents, mapping, security, testing, report and evidence.

Sau mỗi phase: build/test liên quan, `git diff --check`, review staged file list và commit coherent. Không push/merge main trong phạm vi implementation này.

## Validation gates

- Backend: `dotnet build Hosco.slnx --no-restore`.
- Unit: toàn bộ `Hosco.UnitTests`.
- Integration: toàn bộ `Hosco.IntegrationTests`.
- Frontend: `npm.cmd install` và `npm.cmd run build` trong `src/Hosco.Web`.
- Runtime: login, supported intent, ambiguous/unknown, branch scope, alerts, no-data, reporting failure và prompt-injection refusal.
- Regression: Dashboard, exports, Smart Alert, Alert Center, Telegram, RBAC và tenant/branch tests vẫn PASS.

Không ghi PASS nếu command/runtime tương ứng chưa thực sự chạy.

## Definition of Done

- Chat UI và Chat API chạy thực tế.
- Resolver, allowlist guard, authenticated Reporting API client và composer hoạt động.
- Không có chatbot code truy cập database trực tiếp.
- JWT/RBAC/tenant/branch được kiểm tra bằng integration/runtime evidence.
- Unknown, ambiguous, no-data, unavailable và injection cases có safe behavior.
- Không có secret trong repository.
- Full backend tests và frontend build PASS.
- `docs/gd4/*` và `docs/gd4/evidence/*` đầy đủ, không fake screenshot/evidence.

## BA pending

- Final Intent Dataset và canonical utterance list.
- Conversation tone/copy approval.
- LLM provider/vendor và production credential policy.
- Persistent conversation history/retention requirements.
- Whether comparisons beyond explicit period questions are in scope.
