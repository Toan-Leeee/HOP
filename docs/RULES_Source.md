# RULES - PHÂN TÍCH TASK: THỰC HÀNH BUỔI 6 - DATA LAYER VỚI EF CORE

- **Môn:** Lập trình Web Nâng cao - Bài 5: Database với EF Core
- **Đề tài:** Đề tài 07 - Ứng dụng đặt lịch họp trong doanh nghiệp (SRS Nhóm 7)
- **Sinh viên:** Lê Xuân Toàn - 23A1001D0361
- **Thời gian lab:** 120 - 150 phút. Làm theo nhóm BTL (3 SV) + phần cá nhân.
- **Nguồn chuẩn dữ liệu:** SRS mục 6.1 và 6.2. Mọi tên bảng, tên trường, kiểu dữ liệu phải bám SRS.

> Quy ước: **MUST** = bắt buộc theo đề bài hoặc SRS. **SHOULD** = nên làm. **ĐỀ XUẤT** = gợi ý của mình, cần nhóm xác nhận.

---

## 1. Chuỗi Data Layer cần dựng

```
Design Entity -> DbContext -> Migration -> Database -> DbSet -> LINQ -> CRUD + Query
```

Mô hình làm việc với CSDL cần giải thích được:
`Controller -> DbContext -> DbSet -> LINQ Query -> SQL -> Database`

---

## 2. RULES CHUNG (áp dụng cho cả nhóm lẫn cá nhân)

| Mã | Quy tắc | Mức |
|---|---|---|
| R01 | Thực thể phải rút ra **từ SRS**, không tự bịa ngoài phạm vi đề tài. | MUST |
| R02 | Tối thiểu **5-7 thực thể**. | MUST |
| R03 | Có bảng mô tả thực thể: `Entity - Thuộc tính - Kiểu dữ liệu - Ghi chú`. | MUST |
| R04 | Xác định rõ quan hệ (1-n, n-n, optional) và có **ERD** (vẽ tay hoặc phần mềm). | MUST |
| R05 | Mỗi Domain Model có **Primary Key**, **Navigation Property**, **Data Annotation**. | MUST |
| R06 | `DbContext` đặt tại `Data/ApplicationDbContext.cs`, khai báo đủ `DbSet<T>`. | MUST |
| R07 | `ConnectionStrings:DefaultConnection` đặt trong `appsettings.json`. | MUST |
| R08 | Đăng ký DbContext trong `Program.cs` bằng `AddDbContext` + `UseSqlServer`. | MUST |
| R09 | Chạy `Add-Migration InitialCreate` và `Update-Database`; giữ thư mục `Migrations`. | MUST |
| R10 | Có `SeedData.cs` để nạp dữ liệu mẫu. | MUST |
| R11 | CRUD dùng **async** (`ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`...). | MUST |
| R12 | Controller + View tạo bằng **EF scaffold của Visual Studio**. | MUST |
| R13 | Có Query Pipeline: Search `Where()`, Sort `OrderBy()/OrderByDescending()`, Paging `Skip()/Take()`. | MUST |
| R14 | Phần cá nhân phải **giải thích Data Flow** của module mình làm. | MUST |
| R15 | Trả lời 5 câu hỏi lý thuyết (mục 8). | MUST |
| R16 | Tên bảng/trường/kiểu dữ liệu **thống nhất cả 3 thành viên**, theo SRS 6.2. Một DbContext duy nhất cho cả nhóm. | MUST |
| R17 | Không hard delete `User`, `Room`, `Department` đã có lịch sử họp (SRS 6.3) - dùng xóa mềm. | MUST |
| R18 | Không đặt logic nghiệp vụ trong Controller; đưa vào Service nếu có. | SHOULD |
| R19 | Thư mục dự án theo mục 9. Các file dùng chung (`ApplicationDbContext`, `Program.cs`, `SeedData`, `Models/`) chỉ **một người** ghép để tránh xung đột khi gộp code. | SHOULD |

---

## 3. PHÂN TÍCH TASK NHÓM

### G1. Xác định thực thể (R01, R02, R03)

SRS mục 6.1 chỉ liệt kê `Room`, `Meeting`, `MeetingParticipant`. Mục 6.2 thêm `User` (và tham chiếu `Department`, `Role` qua khóa ngoại). Để đủ 5-7 thực thể, **đề xuất 7 thực thể**:

| Entity | Vai trò | Nguồn |
|---|---|---|
| User | Tài khoản người dùng | SRS 6.2 |
| Role | Vai trò Admin (1), Organizer (2), Employee (3) | SRS 6.2 (qua `RoleId`) |
| Department | Phòng ban | SRS 6.2 (qua `DepartmentId`) |
| Room | Phòng họp | SRS 6.1, 6.2 |
| Meeting | Cuộc họp | SRS 6.1, 6.2 |
| MeetingParticipant | Người tham dự và trạng thái RSVP | SRS 6.1, 6.2 |
| AuditLog | Ghi vết thao tác quản trị | **Ngoài SRS** - đã có trong file của Hiếu |

**Bảng Entity đầy đủ theo đúng mẫu của đề (Entity - Thuộc tính - Kiểu dữ liệu - Ghi chú).** Ghi chú "(SRS)" là lấy nguyên từ SRS 6.2; "(bổ sung)" là phần ngoài SRS, cần nhóm xác nhận (R16).

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

### G2. Thiết kế quan hệ + ERD (R04)

| Quan hệ | Loại | Ghi chú |
|---|---|---|
| Role 1 - n User | Bắt buộc | `User.RoleId` |
| Department 1 - n User | Bắt buộc | `User.DepartmentId` |
| Room 1 - n Meeting | Bắt buộc | `Meeting.RoomId` |
| User 1 - n Meeting | Bắt buộc | `Meeting.CreatedByUserId` (người tổ chức) |
| User 1 - n Meeting | **Optional** | `Meeting.CancelledByUserId` (null khi chưa hủy) |
| Meeting 1 - n MeetingParticipant | Bắt buộc | |
| User 1 - n MeetingParticipant | Bắt buộc | Suy ra **Meeting n - n User** qua MeetingParticipant |
| User 1 - n AuditLog | Bắt buộc | `AuditLog.UserId` |

> **Cảnh báo kỹ thuật:** `Meeting` có hai khóa ngoại cùng trỏ về `User`, và `User` còn trỏ tới `MeetingParticipant`. SQL Server sẽ báo lỗi "multiple cascade paths" nếu để mặc định. Cần cấu hình `OnDelete(DeleteBehavior.Restrict)` cho các FK từ `Meeting` đến `User` trong Fluent API. Theo file của Trường: quan hệ Meeting - MeetingParticipant dùng **Cascade**, các khóa ngoại còn lại dùng **Restrict**.

ERD: đã có file `ERD_UC10-UC18.drawio` (vẽ bằng draw.io). Nhóm cần **gộp thành một ERD chung** nếu mỗi người vẽ riêng.

### G3. Tạo Domain Models (R05)

Thư mục `Models/`: `User.cs`, `Role.cs`, `Department.cs`, `Room.cs`, `Meeting.cs`, `MeetingParticipant.cs`, `AuditLog.cs`.

Ánh xạ ràng buộc SRS sang EF Core:

| Ràng buộc SRS | Cách cấu hình |
|---|---|
| Bắt buộc | `[Required]` |
| `nvarchar(200)`, `nvarchar(100)` | `[StringLength(200)]`, `[StringLength(100)]` |
| `varchar(100)`, `varchar(255)` | `[Column(TypeName = "varchar(100)")]` |
| Email, RoomName, DepartmentName duy nhất | Fluent API: `HasIndex(x => x.Email).IsUnique()` (không có Data Annotation tương đương) |
| `Capacity > 0` | `[Range(1, int.MaxValue)]` + `HasCheckConstraint` |
| `EndTime > StartTime` | `HasCheckConstraint` hoặc `IValidatableObject` |
| Khóa chính ghép của MeetingParticipant | Fluent API: `HasKey(x => new { x.MeetingId, x.UserId })` |
| enum `Status` | `HasConversion<string>()` để lưu chữ thay vì số |
| `RowVersion` | `[Timestamp] public byte[] RowVersion` |
| `IsDisabled` mặc định false | `HasDefaultValue(false)` |
| Chỉ mục tăng tốc kiểm tra trùng phòng (file của Trường) | Fluent API: `HasIndex(m => new { m.RoomId, m.StartTime, m.EndTime })` |

Mỗi Model phải có Navigation Property hai chiều (ví dụ `Meeting.Room` và `Room.Meetings`).

### G4. DbContext, kết nối, đăng ký (R06, R07, R08)

- `Data/ApplicationDbContext.cs`: 7 `DbSet<>`, override `OnModelCreating` để đặt các cấu hình ở G3.
- `appsettings.json`: `DefaultConnection` trỏ tới LocalDB hoặc SQL Server của nhóm; **cả nhóm dùng cùng tên database**.
- `Program.cs`: `builder.Services.AddDbContext<ApplicationDbContext>(...)` đọc `GetConnectionString("DefaultConnection")`.

### G5. Migration + Seed (R09, R10)

- `Add-Migration InitialCreate` rồi `Update-Database`.
- **Minh chứng cần chụp:** (1) database được tạo, (2) các bảng sinh đúng, (3) thư mục `Migrations` xuất hiện.
- `SeedData.cs` cần nạp theo thứ tự khóa ngoại: `Role` -> `Department` -> `User` -> `Room` -> `Meeting` -> `MeetingParticipant`.
- Dữ liệu mẫu **đủ để test các module cá nhân**: ví dụ 3 Role, vài Department, ít nhất 1 Admin, 1 Organizer, 1 Employee, vài phòng họp (có 1 phòng `Maintenance`), vài cuộc họp (có cuộc `Cancelled`), vài bản ghi RSVP đủ 3 trạng thái.

---

## 4. PHÂN TÍCH TASK CÁ NHÂN (Lê Xuân Toàn)

Đề bài ghi "mỗi sinh viên thực hiện với module dữ liệu được phân công". Theo bản thiết kế UC10 đến UC18, các module của mình gồm:

| Module | Use Case (số SRS) | Entity chính | Trùng với ai? |
|---|---|---|---|
| M1 - Meeting | UC10, UC11, UC17 | Meeting, MeetingParticipant, Room, User | **Trùng với UC02 của Trường** (cập nhật, hủy); UC17 trùng một phần. Tạo lịch họp (UC09) cũng là UC01 của Trường |
| M1b - RSVP | UC12 | MeetingParticipant, Meeting, User | Không ai làm; chỉ đọc |
| M2 - User | UC13, UC14 | User, Role, Department | **Trùng với UC05 của Hiếu** |
| M3 - Department | UC15 | Department | **Trùng với UC06 của Hiếu** |
| M4 - Room | UC16 | Room | Không trùng (Trường chỉ đọc phòng, Hiếu không làm) |
| M5 - Report | UC18 | Đọc từ Meeting, Room | Không trùng; chỉ đọc, không có CRUD riêng |

> **ĐỀ XUẤT mặc định cho tới khi nhóm xác nhận:** làm **M4 Room** (CRUD đầy đủ, đủ để thể hiện scaffold và Query Pipeline), **M1b RSVP** và **M5 Report** (cả hai chỉ đọc). Đây là ba phần không trùng với Trường, Hiếu và Chiến. Meeting thuộc Trường nên chỉ tham chiếu.
>
> **Cần xác nhận:** nhóm đã chia module nào cho mình? Nếu được giao cả Meeting thì phải thống nhất lại với Trường để không làm trùng.

### P1. Scaffold CRUD (R12) - cho mỗi module được giao

| Chức năng | HTTP | Cách áp dụng |
|---|---|---|
| Index | GET | Danh sách + Query Pipeline (P2) |
| Details | GET | Xem chi tiết một bản ghi |
| Create | GET/POST | Thêm mới |
| Edit | GET/POST | Sửa; với Meeting kèm `RowVersion` |
| Delete | GET/POST | **Xóa mềm** (R17): Meeting đổi `Status = Cancelled`; User/Room/Department đổi `IsDisabled = true` |

Lưu ý riêng cho Meeting: tạo, sửa, hủy cuộc họp thuộc **UC01 và UC02 của Trường** (nhóm trưởng). Nếu không làm module Meeting thì chỉ cần đọc hoặc tham chiếu; nếu có scaffold thì ghi chú "thực hiện ở UC của Trường" như cách Chiến đã làm trong bảng CRUD của bạn ấy.

### P2. Query Pipeline (R13) - cho trang Index

Thứ tự bắt buộc: **Where -> OrderBy -> Count -> Skip/Take -> ToListAsync**. Đếm tổng trước khi phân trang.

| Module | Search `Where()` | Sort | Paging |
|---|---|---|---|
| Meeting (thuộc Trường, chỉ tham chiếu) | Title; RoomId; Status; khoảng StartTime - EndTime; người tổ chức | StartTime tăng/giảm, Title | `page`, `pageSize` |
| MeetingParticipant (RSVP) | MeetingId; Status (Pending / Accepted / Declined); từ khóa tên hoặc email người tham dự | FullName, Status, RespondedAt | `page`, `pageSize` |
| User | FullName hoặc Email; DepartmentId; RoleId; IsDisabled | FullName, Email | `page`, `pageSize` |
| Room | RoomName; Status; Capacity tối thiểu | RoomName, Capacity | `page`, `pageSize` |
| Department | DepartmentName; IsDisabled | DepartmentName | `page`, `pageSize` |

Quy tắc kèm theo:
- Đọc dữ liệu chỉ để hiển thị thì dùng `AsNoTracking()`.
- Tham số `searchString`, `sortOrder`, `page` phải được giữ lại khi chuyển trang.
- Giá trị `page`, `pageSize` không hợp lệ phải có giá trị mặc định, không gây lỗi.

### P3. Giải thích Data Flow (R14)

Mỗi module viết một đoạn hoặc một sơ đồ theo mẫu:

```
View (Index/Edit.cshtml) -> Controller (action) -> DbContext -> DbSet<T>
   -> LINQ (Where/OrderBy/Skip/Take) -> SQL do EF Core sinh ra -> Database
   -> kết quả quay ngược lại -> View hiển thị
```

Với mỗi action, nêu rõ: HTTP method, ViewModel hoặc Entity nhận vào, câu LINQ chính, lệnh SQL tương ứng (SELECT/INSERT/UPDATE), kết quả trả về. Phần này **có thể tái sử dụng** từ mục 14 (MVC CRUD Flow) trong file thiết kế UC10 - UC18.

---

## 5. KẾT QUẢ NỘP BÀI

**Nhóm BTL**
- [ ] ERD hệ thống (chung cả nhóm)
- [ ] Danh sách entity (bảng Entity - Thuộc tính - Kiểu dữ liệu - Ghi chú)
- [ ] `ApplicationDbContext.cs`
- [ ] Thư mục `Migrations` + ảnh chụp
- [ ] Ảnh chụp database và các bảng đã tạo thành công

**Cá nhân**
- [ ] Code module: Controller + View + Model (scaffold)
- [ ] Query Pipeline chạy được (search, sort, paging)
- [ ] Giải thích Data Flow của module
- [ ] Trả lời 5 câu hỏi lý thuyết

---

## 6. THỨ TỰ THỰC HIỆN (ĐỀ XUẤT trong 120 - 150 phút)

| Thời điểm | Việc | Người |
|---|---|---|
| 0 - 20 phút | G1, G2: chốt thực thể, quan hệ, ERD | Cả nhóm |
| 20 - 45 phút | G3, G4: Models, DbContext, kết nối, đăng ký | Chia theo entity, một người ghép |
| 45 - 60 phút | G5: Migration, Update-Database, Seed | Một người làm, cả nhóm kiểm tra |
| 60 - 110 phút | P1, P2: scaffold CRUD + Query Pipeline | Từng người |
| 110 - 140 phút | P3 + 5 câu lý thuyết | Từng người |
| 140 - 150 phút | Đối chiếu checklist mục 5, chụp minh chứng | Cả nhóm |

> **Rủi ro lớn nhất:** ba người làm Model riêng rồi ghép sẽ lệch tên trường (ví dụ `OrganizerId` và `CreatedByUserId`). Chốt G1 trước khi ai bắt đầu viết code.

---

## 7. CÂU HỎI MỞ CẦN XÁC NHẬN

1. Nhóm đã phân công **module dữ liệu** nào cho Toàn (M1 đến M5)?
2. Tên trường thống nhất theo SRS 6.2 (`CreatedByUserId`, `MeetingParticipant.Status`) hay giữ như file của Chiến (`OrganizerId`, `RSVPStatus`)? File của Trường (nhóm trưởng) đã dùng đúng theo SRS.
3. Có giữ `AuditLog` trong DbContext chung không (SRS không có, Hiếu đã dùng)?
4. Các trường bổ sung ngoài SRS (mục G1) có được cả nhóm đồng ý không?
5. Ai chịu trách nhiệm ghép Model, chạy Migration và nộp phần nhóm?
6. Đề liệt kê Delete GET/POST: có chấp nhận **xóa mềm** (đổi `Status` hoặc `IsDisabled`) thay cho xóa cứng không? SRS 6.3 cấm xóa cứng nên mặc định làm xóa mềm, nên hỏi lại giảng viên.
7. Đề ghi nhóm BTL 3 sinh viên nhưng hiện có 4 file cá nhân (Trường, Hiếu, Chiến, Toàn). Nhóm thực tế gồm mấy người, và DbContext chung do ai ghép?

---

## 8. Ý CHÍNH CHO 5 CÂU HỎI LÝ THUYẾT

1. **EF Core là gì?** Là ORM (Object-Relational Mapper) mã nguồn mở của .NET. Cho phép thao tác CSDL bằng đối tượng C# và LINQ thay vì viết SQL thủ công; EF Core tự dịch LINQ sang SQL và ánh xạ kết quả về đối tượng. Dùng tham số hóa nên giúp chống SQL Injection (khớp NFR01.06 trong SRS).
2. **DbContext có vai trò gì?** Là cầu nối giữa ứng dụng và CSDL: giữ kết nối, chứa các `DbSet<T>` đại diện cho bảng, theo dõi thay đổi (change tracking) và ghi xuống DB khi gọi `SaveChanges`. Cấu hình mô hình qua `OnModelCreating`. Được đăng ký bằng Dependency Injection, vòng đời mặc định là Scoped (một request một instance).
3. **Migration dùng để làm gì?** Quản lý phiên bản lược đồ CSDL theo Code First: mỗi lần đổi Model, `Add-Migration` sinh mã mô tả thay đổi, `Update-Database` áp dụng vào DB. Giúp đồng bộ Model với DB, tái tạo DB trên máy khác và có thể quay lui.
4. **Vì sao nên dùng async?** Thao tác DB là I/O chậm; async (`await ... Async()`) trả luồng về thread pool trong lúc chờ, nên server phục vụ được nhiều request đồng thời hơn và không bị nghẽn. Phù hợp yêu cầu hiệu năng và nhiều người dùng đặt lịch cùng lúc.
5. **Khi nào dùng `AsNoTracking()`?** Khi truy vấn chỉ để **đọc/hiển thị** và không sửa rồi lưu lại (danh sách Index, Details, báo cáo thống kê). EF Core không theo dõi thực thể nên nhanh hơn, tốn ít bộ nhớ. Không dùng khi cần cập nhật chính các thực thể đó bằng `SaveChanges`.

---

## 9. CẤU TRÚC DỰ ÁN (ĐỀ XUẤT)

> Kiến trúc theo SRS và các file thiết kế: ASP.NET Core MVC + Service Layer + Entity Framework Core + SQL Server, `IMemoryCache` cho danh mục phòng họp và phòng ban, Fetch API cho các thao tác không reload, Serilog ghi nhật ký. **Tên solution `MeetingScheduler` chỉ là tên giữ chỗ**, nhóm đặt lại cho đúng.

```
MeetingScheduler/
├── MeetingScheduler.sln
├── docs/                                  SRS, ERD, file thiết kế từng thành viên
└── MeetingScheduler.Web/
    ├── Program.cs                         AddDbContext, AddScoped<Service>, AddMemoryCache, Serilog, Session
    ├── appsettings.json                   ConnectionStrings:DefaultConnection, cấu hình SMTP
    ├── Models/                            Entity (R05)
    │   ├── User.cs
    │   ├── Role.cs
    │   ├── Department.cs
    │   ├── Room.cs
    │   ├── Meeting.cs
    │   ├── MeetingParticipant.cs
    │   ├── AuditLog.cs
    │   └── Enums/                         MeetingStatus, RoomStatus, RsvpStatus
    ├── Data/
    │   ├── ApplicationDbContext.cs        7 DbSet + OnModelCreating (R06)
    │   ├── Configurations/                Fluent API từng Entity (khóa ghép, unique, index, check)
    │   └── SeedData.cs                    (R10)
    ├── Migrations/                        sinh bởi Add-Migration (R09)
    ├── ViewModels/
    │   ├── Meetings/                      MeetingEdit, CancelMeeting, RsvpFilter, RsvpTracking...
    │   ├── Rooms/                         RoomViewModel, RoomAvailability...
    │   ├── Users/                         UserCreate, UserEdit, UserFilter, UserLock
    │   ├── Departments/                   DepartmentViewModel
    │   ├── Reports/                       ReportFilter, ReportResult
    │   └── Calendar/ , Email/             CalendarEvent, EmailJob...
    ├── Services/
    │   ├── Interfaces/                    IMeetingService, IRoomAvailabilityService, IRoomService,
    │   │                                  IUserService, IDepartmentService, IReportService, IAuditService
    │   ├── MeetingService.cs
    │   ├── RoomAvailabilityService.cs
    │   ├── RoomService.cs
    │   ├── UserService.cs
    │   ├── DepartmentService.cs
    │   ├── ReportService.cs
    │   ├── AuditService.cs
    │   └── Email/                         EmailQueue, BackgroundEmailService, MailService
    ├── Controllers/                       người dùng đã đăng nhập
    │   ├── AccountController.cs           đăng nhập / đăng xuất
    │   ├── ProfileController.cs           hồ sơ cá nhân, đổi mật khẩu
    │   ├── MeetingController.cs           Index, Details, Create, Edit, Cancel, Rsvp
    │   ├── RoomController.cs              GET /Room/Available (kiểm tra phòng trống)
    │   └── Api/MeetingsApiController.cs   GET /api/meetings (tải lịch qua Ajax)
    ├── Areas/Admin/
    │   ├── Controllers/                   [Authorize(Roles = "Admin")]
    │   │   ├── UserController.cs          UC13, UC14
    │   │   ├── DepartmentController.cs    UC15
    │   │   ├── RoomController.cs          UC16
    │   │   ├── MeetingController.cs       UC17
    │   │   └── ReportController.cs        UC18
    │   └── Views/
    ├── Views/
    │   ├── Shared/                        _Layout.cshtml, _ValidationScriptsPartial
    │   ├── Account/ , Profile/
    │   └── Meeting/                       Index, Details, Create, Edit, Rsvp .cshtml
    ├── Infrastructure/
    │   ├── CacheKeys.cs                   "ROOM_LIST", "DEPARTMENT_LIST"
    │   └── Authorization/                 policy, filter kiểm tra quyền sở hữu cuộc họp
    ├── Logs/                              Serilog (không đưa lên git)
    └── wwwroot/
        ├── js/                            calendar.js, room-available.js, rsvp.js, report-chart.js
        ├── css/
        └── lib/                           bootstrap, jquery, Chart.js
```

### 9.1. Quy tắc đặt tên và phân tầng

| Mã | Quy tắc |
|---|---|
| S01 | Controller mỏng: nhận request, kiểm tra `ModelState`, gọi Service, trả View hoặc JSON. Không viết logic nghiệp vụ và không truy vấn LINQ trực tiếp. |
| S02 | Logic nghiệp vụ, Query Pipeline (Where, OrderBy, Skip, Take) và transaction nằm trong `Services/`. |
| S03 | View và JSON chỉ dùng **ViewModel**, không truyền trực tiếp Entity (chống over-posting). |
| S04 | Mỗi Service có interface trong `Services/Interfaces/`, đăng ký bằng `AddScoped` trong `Program.cs`. |
| S05 | Tên hàm bất đồng bộ kết thúc bằng `Async`, trả về `Task` hoặc `Task<T>`. |
| S06 | Mọi truy vấn chỉ đọc dùng `AsNoTracking()`. |
| S07 | Khóa cache đặt trong `CacheKeys.cs`; sau khi thêm, sửa, khóa phòng ban hoặc phòng họp phải xóa cache tương ứng. |
| S08 | Namespace theo thư mục (ví dụ `MeetingScheduler.Web.Services`). |
| S09 | Không đưa `appsettings.json` chứa mật khẩu thật, thư mục `Logs/`, `bin/`, `obj/` lên git. |

### 9.2. Gợi ý ai sở hữu thư mục nào (ĐỀ XUẤT theo các file thiết kế, cần nhóm xác nhận)

| Phần | Thư mục / file | Người |
|---|---|---|
| Lịch họp: tạo, sửa, hủy, kiểm tra trùng phòng | `MeetingController`, `RoomController` (công khai), `MeetingService`, `RoomAvailabilityService` | Trường |
| Tải lịch qua Ajax, gửi email | `Api/MeetingsApiController`, `Services/Email/`, `wwwroot/js/calendar.js` | Chiến |
| Đăng nhập, hồ sơ, nhân sự, phòng ban | `AccountController`, `ProfileController`, `Admin/UserController`, `Admin/DepartmentController`, `UserService`, `DepartmentService` | Hiếu |
| Phòng họp, theo dõi RSVP, thống kê | `Admin/RoomController`, `Admin/ReportController`, hành động Rsvp, `RoomService`, `ReportService` | Toàn (đề xuất mặc định) |
| **File dùng chung** | `Models/`, `Data/`, `Program.cs`, `appsettings.json` | **Một người ghép** (xem câu hỏi mở số 5) |

> Phần UC17 (`Admin/MeetingController`) và AuditLog đang trùng một phần giữa các file, cần thống nhất người làm.
