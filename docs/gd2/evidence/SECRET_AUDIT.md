# Audit secret và môi trường

Đã thực hiện scan không phân biệt hoa/thường trên các file được track cho các thuật ngữ password, secret, token, API key, signing key, connection string và private key. Giá trị khớp không được sao chép vào evidence này.

Không xác định được production credential. Các tài liệu phát triển/demo được track dưới đây cần được xem xét rõ ràng và không bao giờ được dùng như secret production:

- Potential secret found at `.env.example:5`
- Potential secret found at `src/Hosco.Api/appsettings.json:11`
- Potential secret found at `src/Hosco.Infrastructure/Persistence/DemoSeed.cs:28`
- Potential secret found at `src/Hosco.Api/Hosco.Api.http:17`
- Potential secret found at `tests/Hosco.IntegrationTests/ReportingApiTests.cs:135`
- Potential secret found at `tests/Hosco.IntegrationTests/SeedDatasetTests.cs:13`
- Potential secret found at `README.md:73`
- Potential secret found at `README.md:84`

Connection string SQL Server dùng LocalDB integrated security và không chứa mật khẩu database. Settings và README xác định các giá trị đã commit là placeholder chỉ dành cho phát triển, đồng thời hướng dẫn đưa giá trị local thực vào user-secrets/biến môi trường.

## Quy tắc ignore

Đã xác minh `.gitignore` chặn:

- `.env` và `.env.*`, đồng thời vẫn cho phép `.env.example`
- `appsettings.Local.json`
- `*.pfx` và `*.key`
- `secrets.json`
- mọi thư mục `bin/` và `obj/`

Script xác minh dùng lại chỉ báo vị trí file và dòng, không bao giờ in giá trị khớp. Script không scan Git history hoặc phát hiện secret dựa trên entropy; vẫn nên dùng secret scanner chuyên dụng trong CI.

Kết luận: `PARTIALLY VERIFIED` (Xác minh một phần) vì có quy tắc ignore/kiểm soát cấu hình và không xác định được production credential, nhưng credential demo dùng chung được track trong nhiều file.
