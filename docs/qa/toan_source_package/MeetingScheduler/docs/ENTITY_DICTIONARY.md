# Từ điển thực thể

Nguồn: bảng G1, file `C:\Users\acer\Downloads\RULES_Phan_tich_task_Thuc_hanh_buoi_6_3.md`. Bảng dưới đây chép nguyên nội dung bảng Entity - Thuộc tính - Kiểu dữ liệu - Ghi chú, giữ nhãn SRS/bổ sung. **Bổ sung và AuditLog là baseline suy luận để triển khai, không phải xác nhận của nhóm.** Phân loại SRS dựa trên rules dẫn mục 6.2, không khẳng định đã đối chiếu toàn văn mọi bản SRS.

| Entity | Thuộc tính | Kiểu dữ liệu | Ghi chú |
|---|---|---|---|
| User | UserId | int | PK, tự tăng (SRS) |
| User | FullName | nvarchar(100) | Bắt buộc (SRS) |
| User | Email | varchar(100) | Bắt buộc, duy nhất (SRS) |
| User | Phone | varchar(20) | Không bắt buộc (bổ sung) |
| User | PasswordHash | varchar(255) | Bắt buộc; băm Bcrypt hoặc PBKDF2 (SRS) |
| User | RoleId | int | FK đến Role (SRS) |
| User | DepartmentId | int | FK đến Department (SRS) |
| User | IsDisabled | bit | Mặc định false, xóa mềm (SRS) |
| Role | RoleId | int | PK; 1 = Admin, 2 = Organizer, 3 = Employee (SRS) |
| Role | RoleName | nvarchar(50) | Bắt buộc, duy nhất (bổ sung) |
| Department | DepartmentId | int | PK, tự tăng (bổ sung bảng) |
| Department | DepartmentName | nvarchar(100) | Bắt buộc, duy nhất (bổ sung) |
| Department | IsDisabled | bit | Mặc định false, xóa mềm (bổ sung) |
| Room | RoomId | int | PK, tự tăng (SRS) |
| Room | RoomName | nvarchar(100) | Bắt buộc, duy nhất (SRS) |
| Room | Location | nvarchar(200) | Không bắt buộc (bổ sung; SRS 6.1 có nhắc vị trí) |
| Room | Capacity | int | Bắt buộc, Capacity > 0 (SRS) |
| Room | Equipment | nvarchar(500) | Không bắt buộc (bổ sung; SRS 6.1 có nhắc trang thiết bị) |
| Room | Status | enum | Active / Maintenance (SRS) |
| Room | IsDisabled | bit | Mặc định false, xóa mềm (bổ sung) |
| Meeting | MeetingId | int | PK, tự tăng (SRS) |
| Meeting | Title | nvarchar(200) | Bắt buộc (SRS) |
| Meeting | Description | nvarchar(max) | Không bắt buộc (bổ sung) |
| Meeting | StartTime | datetime | Bắt buộc (SRS) |
| Meeting | EndTime | datetime | Bắt buộc, EndTime > StartTime (SRS) |
| Meeting | RoomId | int | FK đến Room (SRS) |
| Meeting | CreatedByUserId | int | FK đến User, người tổ chức (SRS) |
| Meeting | Status | enum | Confirmed / Cancelled (SRS) |
| Meeting | RowVersion | rowversion | Kiểm soát cập nhật đồng thời, [Timestamp] (bổ sung) |
| Meeting | CancelledAt | datetime | Cho phép null (bổ sung) |
| Meeting | CancelledByUserId | int | FK đến User, cho phép null (bổ sung) |
| Meeting | CancelReason | nvarchar(500) | Cho phép null (bổ sung) |
| Meeting | CreatedAt | datetime | Bắt buộc, mặc định thời điểm tạo (bổ sung, có trong file của Trường) |
| Meeting | UpdatedAt | datetime | Cho phép null (bổ sung, có trong file của Trường) |
| MeetingParticipant | MeetingId | int | PK ghép, FK đến Meeting (SRS) |
| MeetingParticipant | UserId | int | PK ghép, FK đến User (SRS) |
| MeetingParticipant | Status | enum | Pending / Accepted / Declined (SRS) |
| MeetingParticipant | RespondedAt | datetime | Cho phép null; chưa có giá trị khi còn Pending (bổ sung) |
| AuditLog | AuditLogId | int | PK, tự tăng (bổ sung bảng) |
| AuditLog | UserId | int | FK đến User, người thực hiện (bổ sung) |
| AuditLog | Action | nvarchar(100) | Bắt buộc (bổ sung) |
| AuditLog | EntityName | nvarchar(100) | Bắt buộc (bổ sung) |
| AuditLog | EntityId | int | Bắt buộc (bổ sung) |
| AuditLog | CreatedAt | datetime | Bắt buộc (bổ sung) |
| AuditLog | Details | nvarchar(max) | Không bắt buộc (bổ sung) |

## Ánh xạ thực tế cần lưu ý

| Rules | Models/Fluent API hiện tại |
|---|---|
| Status là enum | C# RoomStatus/MeetingStatus/RsvpStatus; SQL nvarchar(20) qua HasConversion<string>(), không phải kiểu SQL tên enum |
| PK ghép | HasKey(MeetingId, UserId) tại DomainConfiguration; không có khóa ID phụ |
| Required/độ dài | Data Annotation; unique index Email/RoleName/DepartmentName/RoomName |
| Capacity và thời gian | CHECK Capacity > 0; CHECK EndTime > StartTime; validation ở ViewModel/Service |
| RowVersion | byte[] có [Timestamp], SQL rowversion sinh tự động; không đưa token tự tạo vào DB |
| PasswordHash | PasswordHasher<User> của ASP.NET Core Identity dùng PBKDF2; không lưu mật khẩu rõ |
| Ngày giờ | Các field DateTime được map datetime; CreatedAt/UpdatedAt/RespondedAt do service gán; không suy ra SQL DEFAULT từ mô tả rules |
| Delete behavior | Meeting → MeetingParticipant Cascade; FK còn lại Restrict; disable không kích hoạt cascade |
| AuditLog.EntityId | ID đối tượng theo EntityName, không phải FK đa hình được SQL Server enforce |

Kiểu nullable trong C#: Phone, Location, Equipment, Description, CancelledAt, CancelledByUserId, CancelReason, UpdatedAt, RespondedAt và Details. Navigation phản ánh quan hệ [ERD.md](ERD.md). ErrorViewModel là ViewModel lỗi template, không phải entity thứ tám hay DbSet.
