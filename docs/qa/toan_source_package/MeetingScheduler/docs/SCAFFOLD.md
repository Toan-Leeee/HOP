# Scaffold CRUD phòng họp

Ngày 14/10/2026, đã thực thi trình scaffold chính thức `dotnet-aspnet-codegenerator` 9.0.0 trên model `Room` và `ApplicationDbContext` của dự án. Lệnh chạy thành công và sinh đủ Controller + Index, Details, Create, Edit, Delete.

```powershell
dotnet aspnet-codegenerator --no-build controller -name RoomScaffoldController -m MeetingScheduler.Web.Models.Room -dc MeetingScheduler.Web.Data.ApplicationDbContext --relativeFolderPath ScaffoldBaseline/Controllers --useDefaultLayout --referenceScriptLibraries --databaseProvider sqlserver
```

Bản sinh tự động được giữ trong `docs/ScaffoldBaseline/` để đối chiếu; không đăng ký thành endpoint của ứng dụng. Mã chạy thực tế ở `Areas/Admin/Controllers/RoomController.cs` và `Areas/Admin/Views/Room/` được điều chỉnh theo rules: controller gọi Service, ViewModel chống over-posting, query pipeline, xóa mềm và audit. Scaffold mặc định truy vấn DbContext trực tiếp và xóa cứng, nên cần các điều chỉnh này để đáp ứng S01–S03/R17.

**R12 ghi rõ scaffold bằng Visual Studio**: đã chạy cùng công cụ scaffold qua CLI; chưa thao tác hộp thoại scaffold trong Visual Studio. Để có minh chứng đúng hình thức giảng viên yêu cầu, trong Visual Studio 2022:

1. Chuột phải thư mục Controllers > Add > New Scaffolded Item.
2. Chọn MVC Controller with views, using Entity Framework.
3. Model class: Room; Data context: ApplicationDbContext.
4. Đặt tên thử nghiệm khác, ví dụ RoomEvidenceController, để không ghi đè controller đang chạy.
5. Chụp màn hình cấu hình và các file được sinh. Sau đó áp dụng Service/ViewModel và xóa mềm như module Room đang có trước khi sử dụng bản scaffold đó.

Không coi bản scaffold nguyên gốc là code cuối đáp ứng đầy đủ rules nghiệp vụ.
