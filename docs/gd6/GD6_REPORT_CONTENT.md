# Nội dung kỹ thuật phục vụ báo cáo thực tập

Bản này cung cấp nội dung kỹ thuật có thể sử dụng trong báo cáo. Thông tin định danh sinh viên được để trong bản cá nhân ngoài repository. Không sửa file Word hiện tại và không tự tạo kết quả nghiệm thu.

## 1. Giới thiệu hệ thống

HOSCO Executive AI Dashboard & Smart Alert Chatbot hỗ trợ người quản lý theo dõi hoạt động bán lẻ qua ba chức năng: xem KPI, xử lý cảnh báo và hỏi báo cáo bằng tiếng Việt. Bản MVP sử dụng dữ liệu mô phỏng, không kết nối dữ liệu khách hàng production.

## 2. Vai trò của DEV

Phần công việc DEV gồm phát triển backend và frontend, xây dựng lớp báo cáo có phân quyền, triển khai cảnh báo và Chatbot, xử lý lỗi Tester, bổ sung regression test và chuẩn bị bàn giao. Đặc tả nghiệp vụ là đầu vào do BA cung cấp. DEV Self-QA không thay thế kiểm thử độc lập của Tester hoặc quyền nghiệm thu của mentor.

## 3. Mục tiêu GD1 đến GD6

GD1 xây dựng đầu vào SRS và từ điển KPI/Alert. GD2 tạo nền tảng dữ liệu, migration, seed và Reporting API. GD3 bổ sung Dashboard cùng Smart Alert. GD4 triển khai Chatbot và adapter LLM. GD5 tích hợp, đối chiếu lỗi Tester và kiểm thử hồi quy. GD6 hoàn thiện hướng dẫn chạy, tài liệu vận hành, dữ liệu demo, rehearsal và hồ sơ chờ nghiệm thu.

Các việc GD6 được chuẩn bị sớm ngày14/10/2026. Thời gian thực tập chính thức vẫn là14/09/2026–14/11/2026. Không ghi các tuần tương lai hoặc buổi nghiệm thu chưa diễn ra thành công việc đã hoàn thành.

## 4. Công nghệ và kiến trúc

Backend sử dụng .NET10 và EF Core. SQL Server là hướng lưu trữ persistent; SQLite RAM phục vụ test/demo. Frontend dùng React, TypeScript và Vite. JWT, PBKDF2 và RBAC kết hợp phạm vi tenant/chi nhánh bảo vệ API. PDFsharp cùng font Noto Sans tạo PDF Unicode; Excel được xuất theo định dạng OOXML.

Chatbot nhận intent đã kiểm tra từ bộ nhận diện cục bộ hoặc adapter OpenAI/Gemini, sau đó gọi Reporting API bằng quyền người dùng. LLM không được tự tạo câu SQL hoặc tự tính ra số liệu KPI.

## 5. Dashboard KPI

Dashboard có8card, lọc ngày và chi nhánh, biểu đồ xu hướng, drill-down, Top/Bottom SKU và tồn kho nguy hiểm. Trong demo tháng1/2026 của HCM, Revenue là14.022.500 và số đơn theo cách tính hiện tại là93. Các câu hỏi về eligibility93/87, rounding, thời điểm hoàn và snapshot tồn kho vẫn chờ BA; DEV không tự đổi công thức để làm test PASS.

## 6. Smart Alert

Năm rule AL-01 đến AL-05 có cấu hình, bằng chứng quan sát, trạng thái xử lý và audit. Scheduler chạy theo chu kỳ15–30phút; alert có cooldown, chống trùng và escalation. Notification delivery được lưu để retry/backoff và kiểm soát idempotency.

Rehearsal GD6 đã kích hoạt AL-03 từ tồn kho seed qua scheduler thật, sau đó acknowledge/resolve trong đúng phạm vi. Kịch bản “6 lần hủy trong5phút” chưa khớp AL-01 hiện tại và không có API ghi thao tác hủy trong MVP; cần mentor/BA xác nhận cách trình diễn thay thế.

## 7. AI Chatbot

Mock hỗ trợ13intent nghiệp vụ hiện hành, câu hỏi nối tiếp và yêu cầu làm rõ khi thiếu kỳ. Số liệu lấy từ API theo quyền; câu ngoài phạm vi không thực hiện reporting query. Các câu prompt injection, yêu cầu secret hoặc truy cập Hà Nội trái quyền bị từ chối.

Adapter live có bounded retry và fallback. Automated tests sử dụng fake HTTP để kiểm tra timeout, lỗi provider và guard. Chúng không chứng minh chất lượng, chi phí hoặc khả năng hoạt động của model live.

## 8. Kiểm thử và bằng chứng

GD6 chạy lại build, Unit, Integration, frontend build/regression, HTTP suite và kiểm tra export. Các kết quả mới nằm trong [báo cáo bàn giao](GD6_FINAL_HANDOFF_REPORT.md), tách rõ với baseline GD5. Clean clone được dùng để kiểm tra việc restore và chạy dự án mà không dựa vào file local ngoài Git.

## 9. Lỗi đã phát hiện và khắc phục

GD5 xử lý pagination, refusal và năm lỗi Self-QA: PDF mất dấu/cắt nội dung, Excel cắt chữ, lưu rule HTTP400, bỏ scope Hà Nội và Logout ẩn trên màn hình hẹp. GD6 fresh restore phát hiện advisory mới của các thư viện crypto; project đã pin Pkcs/Xml10.0.13 và chạy lại regression. Runtime dùng chung của máy vẫn cần IT kiểm tra bản vá riêng.

## 10. Kết quả hoàn thiện GD6

Các tài liệu setup, kiến trúc, ADR, data, runbook, troubleshooting, demo và hồ sơ nghiệm thu đã được chuẩn bị từ source và evidence thật. Demo thực hiện trên local Mock/SQLite, không bypass authentication. Dữ liệu seed có test chạy lại không tạo bản ghi trùng và reset bằng database RAM mới, không xóa production.

## 11. Hạn chế

Chưa có mentor sign-off và một số quyết định BA. SQL Server migration, seed và startup/restart đã được thử trên database test riêng; backup/restore, live LLM, Telegram thật, tải production và SLA chưa được xác minh. UI hiện chưa có badge role/tenant và không có cashier auth role; employee nghiệp vụ khác tài khoản đăng nhập.

## 12. Hướng phát triển

Các hướng tiếp theo gồm Zalo OA, RAG tài liệu dài, streaming và OLAP. Mỗi hướng cần acceptance criteria, quyền truy cập, dữ liệu, ngân sách và kiểm thử riêng. Những mục này là backlog, không được ghi là đã triển khai trong GD6.

## 13. Kiến thức và kinh nghiệm

Qua dự án, việc đặt công thức ở một lớp canonical giúp đối soát giữa Dashboard, Alert và Chat dễ hơn. Negative tests về phạm vi truy cập quan trọng như các trường hợp trả đúng số liệu. Một build PASS chưa chứng minh hệ thống an toàn hoặc đã được nghiệm thu; clean clone và audit dependency có thể phát hiện rủi ro mới sau baseline.

Báo cáo cần phân biệt rõ việc DEV đã làm, việc BA/Tester cung cấp và phần còn chờ mentor. Mỗi kết luận phải có command output, test hoặc ảnh hệ thống thật thay vì chỉ dựa vào checklist.
