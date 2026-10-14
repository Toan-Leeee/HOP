# Cấu trúc và phạm vi triển khai

Nguồn ưu tiên: `C:\Users\acer\Downloads\RULES_Phan_tich_task_Thuc_hanh_buoi_6_3.md`, mục 2, 3 và 9. Tài liệu này mô tả baseline kỹ thuật đang có trong source; không xác nhận thay cho nhóm về phân công hoặc mở rộng SRS.

## Cây thư mục theo rules

```text
MeetingScheduler/
|-- MeetingScheduler.sln
|-- docs/
|   |-- STRUCTURE.md
|   |-- ENTITY_DICTIONARY.md
|   |-- ERD.md
|   |-- DATA_FLOW.md
|   |-- SCAFFOLD.md
|   |-- ScaffoldBaseline/              minh chứng scaffold, không phải endpoint chạy
|   `-- VERIFICATION.md                kết quả chạy do người tổng hợp ghi
|-- MeetingScheduler.Checks/           console kiểm tra SQL Server riêng
`-- MeetingScheduler.Web/
    |-- Program.cs                    DI, MVC, xác thực, Session, Serilog, DbContext
    |-- appsettings.json              DefaultConnection và SMTP
    |-- Models/                       7 entity; Enums/ chứa enum trạng thái
    |-- Data/
    |   |-- ApplicationDbContext.cs    DbContext duy nhất, 7 DbSet
    |   |-- ApplicationDbContextFactory.cs
    |   |-- Configurations/DomainConfiguration.cs
    |   `-- SeedData.cs
    |-- Migrations/                   InitialCreate và snapshot
    |-- ViewModels/                   Account, Common, Departments, Meetings,
    |                                 Rooms, Users, Reports, Calendar, Email
    |-- Services/
    |   |-- Interfaces/ServiceContracts.cs
    |   |-- AccountService.cs, UserService.cs, DepartmentService.cs
    |   |-- RoomService.cs, RoomAvailabilityService.cs
    |   |-- MeetingService.cs, RsvpService.cs, ReportService.cs, AuditService.cs
    |   |-- ServiceSupport.cs
    |   `-- Email/                     queue, background worker, SMTP
    |-- Controllers/                  Account, Profile, Meeting, Room, Api/
    |-- Areas/Admin/                  Controllers/ và Views/
    |-- Infrastructure/               CacheKeys; quyền tại controller/service
    |-- Views/                        Razor dùng ViewModel
    `-- wwwroot/                      css, js, lib
```

Configurations được gom vào `DomainConfiguration.cs` thay vì một file cho mỗi entity; vẫn áp dụng Fluent API bằng `ApplyConfigurationsFromAssembly`. Chỉ một người ghép Models/Data/Program/migration để tránh xung đột R19. Program hiện đăng ký Session và Serilog, ghi console cùng `Logs/app-.log` theo ngày; Logs phải bỏ khỏi Git. Không có thư mục Infrastructure/Authorization riêng: controller dùng Authorize, service kiểm tra actor và quyền sở hữu.

## Phạm vi có trong source

| Module | Contract và chức năng | Giới hạn xác nhận |
|---|---|---|
| Room | IRoomService: CRUD, disable, lookup cache, search/status/capacity, sort/page | Room là module cá nhân mặc định được đề xuất, chưa phải phân công nhóm xác nhận |
| Meeting | IMeetingService: danh sách/chi tiết theo quyền, tạo/sửa/hủy; capacity tính cả organizer, chống overlap; RowVersion | Module dùng chung, rules gắn với Trường |
| Availability | IRoomAvailabilityService: phòng Active, không disabled, đủ sức chứa, không trùng lịch Confirmed | Hai cuộc nối tiếp đúng thời điểm không trùng |
| RSVP | IRsvpService: theo dõi có filter/page; người được mời trả lời Accepted/Declined | Rules cá nhân M1b chỉ đọc; RespondAsync là mở rộng để chạy luồng chung |
| User/Department/Account | CRUD và disable có quyền Admin; đăng nhập, hồ sơ, đổi mật khẩu | Không gán công việc này cho Toàn chỉ vì có source |
| Report | IReportService: tổng lịch, hủy, RSVP, thời lượng theo phòng và khoảng thời gian | Đọc dữ liệu, không có entity Report riêng |
| Audit/Email | Ghi AuditLog; queue và SMTP tùy cấu hình | AuditLog ngoài SRS; có queue không đồng nghĩa gửi email thành công |

Tình trạng build/chạy và minh chứng giao diện được ghi riêng trong [VERIFICATION.md](VERIFICATION.md). Có implementation không tự chứng minh mọi tình huống đã chạy thành công.

## Rules mới và SRS cũ

Đối chiếu dưới đây dựa trên phần rules trích dẫn SRS 6.1, 6.2, 6.3; không phải chứng nhận đã kiểm tra toàn văn mọi phiên bản SRS cũ.

| Nội dung | SRS được rules dẫn lại | Baseline theo rules mới |
|---|---|---|
| Entity | 6.1 nêu Room/Meeting/MeetingParticipant; 6.2 thêm User và FK Role/Department | 7 entity để đáp ứng R02; Role/Department suy ra từ FK; AuditLog ngoài SRS |
| Tên trường | CreatedByUserId và MeetingParticipant.Status | Giữ tên SRS, không đổi thành OrganizerId/RSVPStatus trong bản cá nhân cũ |
| Trường bổ sung | SRS không liệt kê đầy đủ Phone, vị trí/thiết bị, thời điểm, RowVersion, metadata hủy | Được triển khai làm baseline suy luận, xem nhãn bổ sung trong dictionary; chưa có xác nhận nhóm |
| Xóa | SRS 6.3 cấm hard delete dữ liệu có lịch sử | User/Room/Department dùng IsDisabled; Meeting đổi Cancelled, giữ FK và lịch sử |
| Tầng xử lý | Chuỗi Controller → DbContext → LINQ → SQL của bài lab | S01/S02 thêm Service giữa Controller và DbContext, Controller mỏng |
| Payload | Entity là mô hình dữ liệu | S03 dùng ViewModel cho View/JSON và input, chống over-posting |
| Phần cá nhân | Có các UC10–UC18 chồng chéo với file thành viên khác | Room + tracking RSVP + Report là đề xuất mặc định; cần nhóm xác nhận |
| Scaffold R12 | Yêu cầu scaffold bằng Visual Studio | CLI codegenerator dùng engine scaffold tương đương; thao tác VS UI chưa được thực hiện; xem SCAFFOLD.md |
| Cache/Serilog/Fetch | Đề xuất kiến trúc | IMemoryCache cho lookup; Serilog 9.0.0 ghi console và file theo ngày; API lịch trả PagedResult<MeetingDetailsViewModel> |

## Visual Studio 2022 và migration

Theo thông tin môi trường người tổng hợp xác nhận: Visual Studio Community 2022 (biểu tượng tím), phiên bản 17.14.33, hỗ trợ .NET 9. Mở `MeetingScheduler.sln`, chuột phải `MeetingScheduler.Web` → **Set as Startup Project**, chọn profile **http**, nhấn **F5** hoặc **Ctrl+F5**. URL hiện tại: <http://localhost:5086>. Dừng debugger bằng **Shift+F5**.

Trong **Tools → NuGet Package Manager → Package Manager Console**, chọn Default project `MeetingScheduler.Web` và chạy:

```powershell
Update-Database
```

`InitialCreate` đã tồn tại. Không chạy lại `Add-Migration InitialCreate`; khi model thay đổi hãy tạo migration với tên mới. Xem database tại SQL Server Object Explorer, `(localdb)\MSSQLLocalDB` → Databases → MeetingSchedulerLab → Tables. F5 là hướng dẫn sử dụng; không phải tuyên bố đã tự thao tác Visual Studio UI.

## Kiểm tra tích hợp SQL Server

```powershell
dotnet run --project MeetingScheduler.Checks
```

Chạy từ thư mục solution. Không dùng test framework ngoài. Console tham chiếu Web và gọi service thực, dùng migration thực và SQL Server thực. Mỗi lượt tạo `MeetingSchedulerChecks_<timestamp>` qua kết nối master; chỉ migrate database vừa tạo, chỉ drop khi chính lượt chạy đã tạo thành công. Không drop database đã có sẵn, không migrate MeetingSchedulerLab. Có thể đặt `MEETING_CHECKS_CONNECTION` để chọn server/credentials; catalog trong chuỗi này luôn bị thay bằng tên test sinh tự động. Không in mật khẩu. Cleanup lỗi hoặc assertion lỗi đều trả exit code 1. SMTP bị tắt trong harness, HTTP/cookie/antiforgery và VS UI cần minh chứng riêng.
