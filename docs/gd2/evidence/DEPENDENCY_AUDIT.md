# Audit dependency

Ngày audit: 2026-09-15. Các nguồn NuGet được sử dụng là `https://api.nuget.org/v3/index.json` và nguồn package Microsoft SDK đã cài đặt.

## Lỗ hổng

`dotnet list Hosco.slnx package --vulnerable --include-transitive --no-restore` trả exit 0. Cả sáu project đều báo không có package chứa lỗ hổng theo các nguồn hiện tại.

Trạng thái: `VERIFIED` (Đã xác minh) tại thời điểm truy vấn. Kết quả advisory phụ thuộc thời gian và feed.

## Package outdated

`dotnet list Hosco.slnx package --outdated --include-transitive --no-restore` trả exit 0 và báo các phiên bản có thể cập nhật. Không package nào được update.

Các package cấp cao nhất có bản mới:

| Project | Package |
|---|---|
| `Hosco.Api` | `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.11 -> 10.0.12; `Microsoft.EntityFrameworkCore.Design` 10.0.11 -> 10.0.12 |
| `Hosco.Infrastructure` | EF Core Design/InMemory/Sqlite/SqlServer 10.0.11 -> 10.0.12; `System.Security.Cryptography.Xml` 10.0.11 -> 10.0.12 |
| `Hosco.UnitTests` | `coverlet.collector` 6.0.4 -> 10.0.1; `Microsoft.NET.Test.Sdk` 17.14.1 -> 18.10.0; `xunit.runner.visualstudio` 3.1.4 -> 4.0.0 |
| `Hosco.IntegrationTests` | Cùng ba package công cụ test như Unit Test |

Lệnh cũng liệt kê các bản cập nhật bắc cầu, gồm Azure.Identity/Core, Microsoft.Data.SqlClient, EF Core, IdentityModel, Roslyn/MSBuild, SQLitePCLRaw và package test-platform. Chênh lệch major version không phải chỉ dẫn phải update; cần đánh giá khả năng tương thích và release note riêng.
