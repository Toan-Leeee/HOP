# Phần cá nhân Lê Xuân Toàn 23A1001D0361

Module xác nhận theo phân chia: Room, FS01 và FS02 quản lý phòng họp/cơ sở vật chất.

- Báo cáo Data Flow ở PDF để nộp; bản Markdown chứa cùng nội dung để chỉnh sửa.
- ZIP source chứa solution chung chạy được. Module_Room_Files.json chỉ rõ file phần Room.
- Các module User/Department/Meeting/RSVP/Report là thành phần của dự án nhóm, không phải toàn bộ đều thuộc Toàn.
- ServiceContracts.cs và Statuses.cs chứa cả khai báo dùng chung; phần cá nhân trong đó là IRoomService và RoomStatus.
- Mở MeetingScheduler/MeetingScheduler.sln bằng VS2022, chọn Web làm Startup Project.
- Chọn http và F5; đăng nhập admin@company.test / Demo@12345; vào /Admin/Room.
- LocalDB (localdb)\MSSQLLocalDB; migration và seed chạy tự động trong Development.
- Khi cổng 5086 đã có tiến trình dự án, dừng nó trước khi F5. README solution có hướng dẫn.
- Trong Package Manager Console: Update-Database; không tạo lại InitialCreate.
- Không có bin/obj/Logs/database file trong ZIP. Có Migrations, SeedData và cấu hình kết nối không chứa mật khẩu thật.
- Scaffold đã chạy qua CLI; minh chứng hộp thoại VS chưa thực hiện, xem docs/SCAFFOLD.md.
