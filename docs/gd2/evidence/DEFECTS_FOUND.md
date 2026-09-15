# Các defect đã phát hiện

Không có business code nào bị thay đổi. Các finding sau được giữ lại để chủ sở hữu phân loại xử lý.

## GD2-DEF-01 — Toàn vẹn tham chiếu Branch chưa đầy đủ

- Mức độ nghiêm trọng: Medium
- File: `src/Hosco.Infrastructure/Persistence/HoscoDbContext.cs:44-46`; Migration đầu tiên
- Vấn đề: `Refund.BranchId`, `Alert.BranchId` và `AuditLog.BranchId` không có khóa ngoại vật lý tới `Branches`. Các khóa ngoại Tenant và Branch tách biệt ở nơi khác cũng không đảm bảo Branch được chọn thuộc Tenant được chọn.
- Minh chứng: `sys.foreign_keys` của SQL Server chỉ chứa Branch FK cho Employees, Inventories, Orders và UserBranches.
- Đề xuất sửa: Xác định các tham chiếu Branch lịch sử có thể null có phải chủ đích hay không; nếu không, bổ sung quan hệ và ràng buộc tổng hợp đảm bảo nhất quán Tenant trong Migration mới.

## GD2-DEF-02 — Liveness khi cold start phụ thuộc Database Migration

- Mức độ nghiêm trọng: Medium
- File: `src/Hosco.Api/Program.cs:25-29,145-151`
- Vấn đề: `InitializeDatabaseAsync` chạy `MigrateAsync` trước `RunAsync`. Nếu SQL Server không khả dụng khi cold start, process không lắng nghe và `/health/live` không thể báo trạng thái process, dù bản thân live check không phụ thuộc dependency.
- Minh chứng: Kiểm tra trực tiếp luồng code; Runtime dùng SQL khỏe mạnh đã được xác minh nhưng LocalDB instance dùng chung không bị cố tình dừng.
- Đề xuất sửa: Chạy Migration production như một bước phát hành, hoặc cho host lắng nghe trong khi readiness vẫn unhealthy và retry quá trình khởi tạo một cách an toàn.

## GD2-DEF-03 — Semantic preview bị BA chặn có thể trông như dữ liệu chính thức

- Mức độ nghiêm trọng: Medium
- File: `src/Hosco.Application/Semantics/QueryCatalog.cs:20-30`; `src/Hosco.Api/Controllers/ReportingController.cs:37-72`; `src/Hosco.Infrastructure/Persistence/ReportingDataStore.cs`
- Vấn đề: Query ID cho revenue, product-ranking và dangerous-inventory được đánh dấu `BlockedByBusinessDefinition`, nhưng endpoint vẫn trả response 200 đã tính toán mà không có trường trạng thái định nghĩa. Người dùng có thể nhầm quy tắc provisional là semantic KPI đã phê duyệt.
- Minh chứng: Đã đối chiếu trực tiếp trạng thái catalog với implementation controller/data store; Runtime xác nhận các route thực thi được.
- Đề xuất sửa: Sau khi BA phê duyệt, nâng trạng thái định nghĩa cùng công thức có version; trước thời điểm đó, công khai trạng thái provisional trong từng response hoặc ngăn sử dụng cho nghiệp vụ.

## GD2-DEF-04 — Credential demo dùng chung được track trong nhiều file

- Mức độ nghiêm trọng: Low
- File: Các vị trí liệt kê trong `SECRET_AUDIT.md`
- Vấn đề: Credential được mô tả là chỉ dành cho phát triển và SQL chỉ lưu hash, nhưng giá trị đầu vào dùng chung xuất hiện trong source/test/example; điều này kích hoạt secret scanner hoặc tạo nguy cơ tái sử dụng nhầm.
- Minh chứng: Scan file được track chỉ ghi vị trí; giá trị không được lặp lại tại đây.
- Đề xuất sửa: Truyền mật khẩu Seed/Test qua biến môi trường hoặc cấu hình test và chỉ giữ placeholder không bí mật trong ví dụ được track.

## GD2-DEF-05 — Truy vấn login phát cảnh báo multiple-collection include

- Mức độ nghiêm trọng: Low
- File: `src/Hosco.Infrastructure/Security/IdentityStore.cs:11-14`
- Vấn đề: Login tải role và Branch dưới dạng hai collection include với hành vi single-query mặc định, có thể tạo tích Descartes khi số lượng phân công tăng.
- Minh chứng: EF Core phát `MultipleCollectionIncludeWarning` trong lần xác minh API Runtime dùng SQL.
- Đề xuất sửa: Dùng projection hoặc chiến lược split-query được đánh giá rõ ràng sau khi đo tải thực tế.
