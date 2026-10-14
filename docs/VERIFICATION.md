# Kết quả kiểm tra ngày 14 tháng 10 năm 2026

Các kết quả dưới đây do Codex chạy trực tiếp trên máy trong phiên làm việc này. Không trình bày các kết quả này là thao tác do sinh viên tự thực hiện.

## Build và môi trường

- Visual Studio Community 2022 17.14.33; workload ASP.NET and web development được phát hiện bằng vswhere.
- Solution chuẩn Visual Studio 17: MeetingScheduler.sln, gồm Web, Checks và thư mục docs trong Solution Explorer.
- .NET SDK 9.0.314; project net9.0; EF Core SQL Server 9.0.10.
- `dotnet build MeetingScheduler.sln`: thành công, 0 warning, 0 error.
- MSBuild của Visual Studio 2022 build Debug solution thành công. Chưa chạy debugger F5 bên trong giao diện IDE.
- Dev server thực tế lắng nghe http://localhost:5086, profile http, môi trường Development.

## Database và migration

Đã chạy `dotnet ef migrations add InitialCreate` và `dotnet ef database update`, tương đương Add-Migration/Update-Database trong Package Manager Console. SQL Server LocalDB xác nhận migration `20261014101256_InitialCreate` trong __EFMigrationsHistory.

Đã kiểm tra trực tiếp sys.tables và INFORMATION_SCHEMA.COLUMNS bằng sqlcmd: đúng 7 bảng nghiệp vụ, 45 cột; User.Email varchar(100), PasswordHash varchar(255); Meeting.RowVersion timestamp/rowversion; thời điểm datetime; các status lưu chữ nvarchar(20).

| Bảng | Số bản ghi sau seed, trước kiểm tra đăng nhập trình duyệt |
|---|---:|
| Role | 3 |
| Department | 3 |
| User | 5 |
| Room | 3 |
| Meeting | 3 |
| MeetingParticipant | 9 |
| AuditLog | 0 |

AuditLog tăng khi đăng nhập hoặc thay đổi dữ liệu. Các kiểm tra trình duyệt không xóa/sửa dữ liệu mẫu; form trùng lịch bị từ chối trước khi lưu.

## Kiểm tra tích hợp SQL Server

Chạy `dotnet run --project MeetingScheduler.Checks --no-build`: **9 passed, 0 failed**.

1. Room CRUD, tìm kiếm/sắp xếp/phân trang, chuẩn hóa tham số, unique/capacity, invalidation cache.
2. Quyền quản trị và quyền đặt lịch của các vai trò.
3. Đăng nhập đúng/sai mật khẩu, khóa/mở khóa, thu hồi session khi khóa, đổi mật khẩu và từ chối mật khẩu cũ.
4. Tạo lịch, từ chối trùng phòng, chấp nhận lịch nối tiếp, sức chứa và truy vấn phòng trống.
5. RowVersion cũ bị từ chối giữa hai DbContext độc lập; quyền sửa/hủy.
6. Hai yêu cầu đặt trùng phòng đồng thời có đúng một yêu cầu thành công.
7. Pending/Accepted/Declined, thời điểm phản hồi, filter/paging, từ chối người không được mời và status lạ.
8. Báo cáo đúng số lịch, trạng thái, thời lượng và RSVP so với dữ liệu SQL thực tế.
9. Hủy lịch/xóa mềm Room/User/Department giữ lịch sử, khóa ngoại và AuditLog.

Database kiểm tra được tạo riêng: `MeetingSchedulerChecks_202610141027385851137`. Sau kiểm tra đã xóa database này và xác nhận không còn trong sys.databases. Không sử dụng database demo để chạy các kiểm tra thay đổi dữ liệu.

## Kiểm tra trình duyệt

Playwright với Microsoft Edge, desktop 1440 x 1000 và mobile 390 x 844: **90 passed, 0 failed**. Kết quả chi tiết: [qa/browser-results.json](qa/browser-results.json).

Đã mở các trang CRUD quản trị, tạo/sửa/chi tiết/hủy lịch, theo dõi RSVP, hồ sơ, đổi mật khẩu và báo cáo. Đã kiểm tra đăng nhập/đăng xuất, API lịch, form trùng lịch trả validation, phòng bận bị loại khỏi lựa chọn, tìm kiếm/phân trang HTML, calendar 7 ngày và chuyển tuần, menu mobile, quyền Employee và POST thiếu antiforgery bị chặn. Không có lỗi JavaScript; các viewport đã kiểm tra không tràn chiều ngang trang.

Đã xem ảnh desktop/mobile sau render để kiểm tra bố cục. Ảnh trong `docs/qa/`: login-desktop.png, meetings-desktop.png, meetings-mobile.png, calendar-desktop.png, calendar-mobile.png, report-desktop.png.

## Giới hạn và minh chứng cần bổ sung khi nộp

- R12 yêu cầu thao tác scaffold trong Visual Studio. Phiên này đã chạy CLI `dotnet-aspnet-codegenerator` chính thức và giữ code sinh tại docs/ScaffoldBaseline; chưa thao tác hộp thoại scaffold trong IDE. Hướng dẫn và phạm vi điều chỉnh nằm trong SCAFFOLD.md.
- Chưa có ảnh chụp SQL Server Object Explorer/Package Manager Console của Visual Studio. Database và schema đã xác nhận bằng truy vấn thật; hướng dẫn chụp trong README.
- SMTP chưa được cấu hình, chưa xác nhận gửi email thật. Hàng đợi email là best effort trong bộ nhớ, không có outbox bền vững/retry; lịch họp lưu thành công không đồng nghĩa email đã giao.
- Phân công thành viên, AuditLog và các trường bổ sung vẫn là baseline theo rules cần nhóm thống nhất; không xem việc code xong là xác nhận phân công.
- Đây là ứng dụng bài thực hành chạy local; chưa kiểm tra triển khai production, tải lớn hay toàn bộ trường hợp trên mọi trình duyệt.
