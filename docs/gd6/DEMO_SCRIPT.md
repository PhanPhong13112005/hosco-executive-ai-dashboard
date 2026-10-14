# Kịch bản demo có thể chạy lại
Scope: local Mock/SQLite RAM, source revision từ GD6handoff. Tổng dự kiến12–15 phút, không SLA đo thực tế. [Runbook](OPERATIONS_RUNBOOK.md) setup/start; đặt RunOnStartup=true trước backend, không đổi clock/threshold.
| Bước | Thao tác / input | Expected | Dự kiến | Evidence / actual |
|---|---|---|---|---|
| 1 Login | Owner fixture, Backend thật | login success, branch dropdown chỉHCM; role được đối chiếu account guide/API, không badge UI giả | 1min | rehearsal report |
| 2 Dashboard | Jan1–31/2026, HCM, Apply | Revenue14,022,500/GMV14,062,500/Orders93, trend/ranking scoped | 2min | dashboard-desktop.jpg |
| 3 Drill-down | Revenue card | cùng kỳ/scope, chart/value | 1min | runtime/API assertions |
| 4 Empty | Jan2030 | ratiosN/A, totals0, current stock2 | 1min | HTTP+formatter regression |
| 5 Alert | xem AL03 newly detected hôm nay, detail, ack/resolve với note | available1/safety8; Open->Ack->Resolved; TelegramSkipped vìdisabled | 2min | alerts-scheduler.jpg, alert-resolved.jpg |
| 6 Rules | AL01 Save không thay numeric defaults | Unicode đúng; không400; PENDING giữ nguyên | 1min | rules-save.jpg + HTTPregression |
| 7 AI | RevenueJan; today; thiếu kỳ; follow-up; Hanoi; weather; injection/secret | canonical Jan value; today noData; clarification; authorized scopes; forbidden/noQuery | 3min | chat evidence + HTTP suite |
| 8 Scope | OwnerHanoi denied; ChainHanoi12,307,500; tenantB isolation | 403 hoặc đúng allowed data, không tenant leakage | 1min | HTTP + chat-hanoi evidence |
| 9 Export | nút Excel/PDF | download thật, Vietnamese, full text, same filters/data; deniedscope403 | 1min | binary/exportchecks/render |
| 10 Logout | desktop1280×900 và mobile390×844 | visible button; click -> login; auth-required API401 | 1min | logout-desktop/mobile screenshots |
## Sai lệch kế hoạch
“6 thao tác hủy trong5phút” **BLOCKED / PENDING BA ALIGNMENT**: source AL01 so tỷ lệ1day với7daybaseline; không cancellation mutation API trong read-only reporting MVP. Không viết API giả/resetthreshold đểdemo. AlternativeAL03 dùng dữ liệu tồn thực của seed, đã kích hoạt bằng scheduler. Mentor phải xác nhận alternative đáp ứng demo acceptance.
Câu “hôm nay” đúng nhưng empty khi nguồn historical; demo số thực dùng explicitJanuary. Không fabricate current sales. UI role badge và cashier account không tồn tại: disclose thay vì giả dữ liệu.
## Outage/fallback
Không dùng token live thật. Full Integration fake HTTP kiểm timeout429/5xx/schemafailure/missingcredentials, fallbackbounded/noSQL; case outage simulator không phải live provider outage. Live demo NOT VERIFIED nếu chưa credentials/approval.
Rehearsal status/actual được ghi tại [report](DEMO_REHEARSAL_REPORT.md).
