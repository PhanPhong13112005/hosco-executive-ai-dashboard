# Checklist trước buổi nghiệm thu
Owner chuẩn bị: DEV. Quyền nghiệm thu: BA/Tester/mentor; chưa ai được ghi đã duyệt.
| Hạng mục | Evidence | Trạng thái |
|---|---|---|
| README/setup/env/OpenAPI | clean clone setup + Swagger routes test | xem final gates |
| ADR/runbook/data docs | architecture/ADR/data linked sources | prepared; SQL migrations/startup/restart PASS, backup/restore chưa kiểm chứng |
| Demo seed/roles repeat | Gd6DemoReadinessTests +5role smoke | xem final gates |
| Dashboard/ranking/N-A | HTTP167 +GD6smoke +Nodeformatter | xem final gates |
| SmartAlert5rules/workflow | actual AL03scheduler + Unit/Integration/HTTP | source behavior tested; BAdefaults PENDING |
| Chat Mock/security/scope | UI + fakeprovider/API tests | local verified; scope/default oracle BA pending |
| PDF/XLSX | actual UI download + content/render tests | xem export evidence |
| responsive logout | desktop/mobile UI + Node regression | xem rehearsal |
| NuGet audit | crypto10.0.13/no vulnerable graph | package-level only; machine runtimepatch pendingIT |
| original6cancel scenario/cashier role | plan vsCatalog mismatch | PENDING APPROVAL / unsupported current MVP |
| liveLLM/Telegram/loadSLA | thiếu credentials hoặc chưa có load benchmark | NOT VERIFIED |
| Mentor demo/feedback/signature | empty notes awaiting actual meeting | PENDING APPROVAL |
Không tick một artifact “prepared” thành mentor acceptance PASS. Kết luận5tasks cụ thể ở [handoff](GD6_FINAL_HANDOFF_REPORT.md).
