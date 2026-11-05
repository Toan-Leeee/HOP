# Ứng dụng đặt lịch họp doanh nghiệp

Dự án ASP.NET Core MVC (.NET 9), Entity Framework Core 9 và SQL Server LocalDB, mở bằng **Visual Studio 2022**. Đã kiểm tra Visual Studio Community 2022 17.14.33 và workload ASP.NET trên máy hiện tại.

## Mở và chạy trong Visual Studio 2022

1. Mở `MeetingScheduler.sln`.
2. Chuột phải `MeetingScheduler.Web` > **Set as Startup Project**.
3. Chọn profile **http** trên thanh Run, bấm **F5** hoặc **Ctrl+F5**.
4. Trong Development, ứng dụng áp dụng migration và nạp dữ liệu demo vào database `MeetingSchedulerLab` khi khởi động.
5. Truy cập `http://localhost:5086`. Dừng ứng dụng bằng **Shift+F5**.

Nếu bản chạy nền đang dùng cổng 5086, chạy `powershell -ExecutionPolicy Bypass -File scripts/Stop-Dev.ps1` từ thư mục solution trước khi F5. Script chỉ dừng tiến trình chạy từ thư mục bin của dự án này.

Tài khoản mẫu (chỉ được seed trong Development):

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | admin@company.test | Demo@12345 |
| Organizer | organizer@company.test | Demo@12345 |
| Employee | employee@company.test | Demo@12345 |

## Migration trong Visual Studio

Mở **Tools > NuGet Package Manager > Package Manager Console**, chọn Default project `MeetingScheduler.Web`:

```powershell
Update-Database
```

Migration `InitialCreate` đã có trong source. Khi thay đổi model sau này, tạo migration mới:

```powershell
Add-Migration TenThayDoiMoi
Update-Database
```

Trong SQL Server Object Explorer, kết nối `(localdb)\MSSQLLocalDB`, mở Databases > MeetingSchedulerLab > Tables để chụp minh chứng database và 7 bảng.

## Chạy bằng terminal

Từ thư mục chứa solution:

```powershell
dotnet tool restore
dotnet restore
dotnet ef database update --project MeetingScheduler.Web
dotnet run --project MeetingScheduler.Web --launch-profile http
```

Dừng terminal bằng Ctrl+C. Kết nối được đặt ở `MeetingScheduler.Web/appsettings.json`, khóa `ConnectionStrings:DefaultConnection`. Có thể thay LocalDB bằng SQL Server của nhóm.

## Cấu trúc và luồng dữ liệu

Xem `docs/STRUCTURE.md`, `docs/ENTITY_DICTIONARY.md`, `docs/ERD.md`, `docs/DATA_FLOW.md`.

Controller nhận ViewModel, kiểm tra ModelState, gọi Service. Service xử lý nghiệp vụ, DbContext và LINQ. View/JSON chỉ sử dụng ViewModel. Query Pipeline: Where -> OrderBy -> CountAsync -> Skip/Take -> ToListAsync.

Code chung gồm 7 entity và một DbContext. Các module cá nhân theo đề xuất trong rules là Room, theo dõi RSVP và Report; solution cũng có các module dùng chung để chạy luồng đặt lịch từ đầu đến cuối. Phân công nhóm và các trường bổ sung vẫn là giả định kỹ thuật theo file rules, không phải xác nhận của các thành viên.

## Email

Mặc định SMTP chưa cấu hình; lịch họp vẫn được lưu và không báo email đã gửi. Muốn gửi thư thật, đặt `Smtp:Host`, `Smtp:From`, `Smtp:Username`, `Smtp:Password` bằng User Secrets hoặc biến môi trường. Không ghi mật khẩu thật vào appsettings hoặc Git. Hàng đợi email trong bộ nhớ phục vụ demo, không bảo đảm giao thư sau khi khởi động lại.

## Kiểm tra

`MeetingScheduler.Checks` là chương trình kiểm tra tích hợp với database SQL Server riêng. Xem kết quả đã chạy trong `docs/VERIFICATION.md`.

```powershell
dotnet run --project MeetingScheduler.Checks
```

Tài liệu scaffold và cách tạo lại bằng Visual Studio được ghi trong `docs/SCAFFOLD.md`. Phiên bản giao diện và controller sau scaffold được điều chỉnh để dùng Service/ViewModel và xóa mềm theo rules.

Tham khảo kỹ thuật: [Microsoft MVC EF Core tutorial](https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-mvc-app/adding-model?view=aspnetcore-9.0).
