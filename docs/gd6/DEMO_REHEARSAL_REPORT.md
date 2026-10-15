# Kết quả chạy thử demo GD6

Ngày chạy: 14/10/2026, khoảng 15:07–15:32 UTC+7 cho thao tác trình duyệt; HTTP regression chạy lại sau đó. Người thực hiện: DEV Self-QA, không phải mentor nghiệm thu. Branch: fix/gd5-tester-findings; nguồn ứng dụng là baseline84b9414 cộng patch crypto và metadata Swagger của GD6.

Môi trường: frontend127.0.0.1:5173, API5000, SQLite RAM, Mock AI, scheduler thực với RunOnStartup=true, Telegram disabled. JWT login thật; không bypass authentication. Desktop1280×900, mobile390×844. Thời lượng trong bảng là ước tính cho buổi trình diễn, không phải SLA đo từ browser automation.

| Thao tác / input | Expected | Actual | Trạng thái | Evidence | Dự kiến |
|---|---|---|---|---|---|
| Owner login | Backend cấp quyền HCM | Đăng nhập thành công, dropdown chỉ HCM | PASS | dashboard-desktop.jpg; HTTP login/scope | 1 phút |
| Dashboard 01–31/01/2026 HCM | 8 cards, trends và ranking đúng scope | Revenue14.022.500, GMV14.062.500, Orders93, GP6.127.500, AOV hiển thị150.780, Margin43,7%, stock2 | PASS | dashboard-desktop.jpg; HTTP canonical fields | 2 phút |
| Drill-down Revenue/AOV | Endpoint cùng kỳ, tenant/branch | HTTP200; chưa click lại drill-down trong UI GD6 | PASS API / NOT VERIFIED UI retest | http-regression.json | 1 phút |
| Kỳ tháng1/2030 | Totals0, AOV/Margin N/A | API null ratios; Excel/PDF N/A; Node formatter PASS; chưa chụp empty UI mới | PASS API/export / NOT VERIFIED UI retest | export checks; dashboard-empty-final.pdf/xlsx | 1 phút |
| Alert Center, AL03 mới | Scheduler sinh alert từ available1/safety8 | Hai alert HCM mới mang timestamp ngày14/10, không chỉ historical fixtures | PASS | alerts-scheduler.jpg; smoke-gd6.json | 1 phút |
| AL03 detail, acknowledge rồi resolve | Persist trạng thái và note trong RAM hiện tại | Resolved, có audit actor/time/note; delivery Initial Telegram Skipped, attempt1 | PASS | alert-resolved.jpg | 1 phút |
| AL01 Save, giữ numeric defaults | Không400, Unicode đúng, vẫn BA pending | Lưu không hiển thị lỗi; HTTP save200, config equality được kiểm tra riêng | PASS | rules-save.jpg; http-regression.json | 1 phút |
| Chat Revenue tháng1, hỏi hôm nay | Số canonical; hôm nay ngoài dữ liệu lịch sử phải no-data | 14.022.500 VND; today trả không có dữ liệu | PASS | chat-clarification-followup.jpg; HTTP | 1 phút |
| Thiếu kỳ, hỏi nối tiếp tuần trước, weather | Clarify / scoped follow-up / không query ngoài phạm vi | DOM đã kiểm tra và HTTP xác nhận Ambiguous, follow-up200, Unknown/noQuery | PASS | smoke-gd6.json; ảnh chat chỉ chứng minh Jan/today, không chụp đủ các lượt này | 1 phút |
| Injection, yêu cầu secret | Từ chối, không tiết lộ dữ liệu | UI lỗi permission; HTTP403/noData | PASS | chat-security.jpg; HTTP negative cases | 1 phút |
| Owner hỏi Hà Nội | Denied, không bỏ scope | UI permission error; HTTP403 | PASS | owner-hanoi-denied.jpg | 30 giây |
| ChainManager hỏi Hà Nội | Allowed HN, đúng tenant | 12.307.500 VND qua UI thật; cross-tenant403 được HTTP kiểm riêng | PASS | chain-hanoi-mobile.jpg; HTTP | 1 phút |
| Nút Excel/PDF với bộ lọc tháng1 | Download thật, đúng dấu và không cắt chữ | Hai file UI được tải; PDF render đầy đủ đến dòng stock cuối, XLSX metadata và KPI không bị cắt | PASS | dashboard-ui.pdf/xlsx; dashboard-pdf-preview.png; dashboard-xlsx-preview.png | 1 phút |
| Logout desktop và mobile | Nút nhìn thấy, bấm trở về login | Cả hai viewport thực hiện thành công; đổi tài khoản không còn history của Owner | PASS | logout-desktop.jpg, logout-mobile.jpg, chain-hanoi-mobile.jpg | 1 phút |
| Provider outage/fallback | Bounded retry, no arbitrary SQL | Automated fake HTTP Integration PASS; không thử live outage | PASS fake / NOT VERIFIED live | full backend validation | 1 phút |
| “6 lần hủy trong5phút” | Theo kế hoạch cũ | Không khớp AL01 day/baseline và không có cancellation-write API; trình diễn AL03 thay thế chưa được BA/mentor duyệt | BLOCKED / PENDING APPROVAL | Alert Catalog; DEMO_SCRIPT.md | Chờ chốt |

Không thêm cashier auth role hoặc UI role badge giả. Quyền được chứng minh bằng account mapping, directory API và negative tests. UI có 8 cards, không phải 8 mã KPI độc lập; KPI07 ranking ở khu vực riêng. “Today no-data” đúng với seed Jan–Jun, không chỉnh ngày máy để làm demo đẹp.

Kết luận: các luồng local đã nêu PASS; kịch bản nguyên bản và live integrations chưa hoàn thành nghiệm thu. Không gọi cả demo là PASS không điều kiện. Console browser error/warn query tại thời điểm kiểm tra trả danh sách rỗng, không phải chứng minh mọi lượt đều không có warning.

Sau rehearsal chỉ dừng các server do DEV khởi động. RAM workflow mất khi restart theo thiết kế; SQL test persistence được kiểm tra riêng. Ảnh là capture thật; exports chỉ dữ liệu synthetic, không JWT/key/password thật.
