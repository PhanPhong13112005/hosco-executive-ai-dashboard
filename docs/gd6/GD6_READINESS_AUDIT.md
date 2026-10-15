# Audit mức độ sẵn sàng GD6
## Phạm vi và nguồn
Audit ngày 14/10/2026 trên branch fix/gd5-tester-findings, baseline GD5 84b94141bb9afc1a1a3cb16e58e3dcc2979129e3. Working tree đầu lượt sạch; actual origin feature HEAD trùng baseline. Không xử lý main.
Đã đọc kế hoạch trong bản local “HOSCO - Kế hoạch thực tập _ Executive AI Dashboard & Smart Alert Chatbot.xlsx”, sheet “GD6 - HOÀN THIỆN & NGHIỆM THU”, các dòng 5–9. Chủ dự án đã xác nhận dùng bản không có hậu tố (1) ngày 14/10/2026. Không sửa workbook. Kế hoạch ghi tuần 11–12/mốc 09–14/11; đây là chuẩn bị sớm ngày 14/10, không đổi thời gian thực tập chính thức 14/09–14/11/2026 hay ghi các tuần tương lai đã hoàn thành.
Sources: SRS v1 và KPI Dictionary local GD1; [BA decisions](../testing/BA_DECISIONS.md); [GD5 Self-QA](../testing/GD5_SELF_QA_REPORT.md); [GD5 handoff](../testing/GD5_HANDOFF_REPORT.md); catalogs trong source và [Alert Catalog](../gd3/ALERT_RULE_CATALOG.md).
## Kết quả rà soát
| Phần | Trạng thái audit | Bằng chứng / khoảng trống |
|---|---|---|
| Dashboard/reporting/export | Implementation có, cần validation GD6 mới | 8 cards + ranking, filters/drilldown/Unicode exports; không dùng số GD5 làm số mới |
| Alerts | Implementation có | AL01–05, scheduler/delivery/workflow; defaults vẫn BA đề xuất |
| AI | Mock và adapter có | Local resolver/structured provider/Reporting HTTP boundary; live chưa có môi trường |
| Auth/scope | Có regression | 4 SystemRole; Owner/Admin chỉ A-HCM, không tự mở tenant |
| DB | SQLite và SQL Server test được kiểm chứng GD6 | SQL test riêng đã migrate 4 bản/startup/restart; backup/restore chưa kiểm chứng. SQLite EnsureCreated là luồng khác |
| Documentation | Có điểm sai, được sửa | README thiếu đúng frontend quick-start, user-secrets không auto-loaded, sai role scope/seed; Tester setup branch/counts cũ |
| Security dependencies | Phát hiện mới | Clean clone restore báo9 NU1902/NU1903 crypto warnings; pin Pkcs/Xml10.0.13, audit lại không báo vulnerable package |
| Machine runtime | PENDING IT PATCH | .NET/ASP.NET shared runtime10.0.11; NuGet patch không chứng minh shared framework đã được vá |
| Demo kế hoạch | PARTIAL | Không cashier role; không cancellation-write API; “6 hủy/5 phút” không khớp AL01 hiện tại |
| Mentor acceptance | PENDING APPROVAL | Chưa có mentor demo/feedback/sign-off |
## Quy tắc nghiệm thu
Giữ nguyên BA-GD5-01..10 PENDING. Source label Implemented không phải chữ ký BA; kỳ Jan/HCM orders93 là current implementation, không ép thành87. Không đưa dữ liệu cá nhân/báo cáo Word/credentials thật lên Git.
[Validation mới và kết luận](GD6_FINAL_HANDOFF_REPORT.md) là trạng thái cuối, không thay baseline audit này thành production PASS.
