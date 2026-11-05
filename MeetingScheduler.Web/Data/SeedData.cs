using MeetingScheduler.Web.Models;
using MeetingScheduler.Web.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MeetingScheduler.Web.Data;

public static class SeedData
{
    private const string DemoPassword = "Demo@12345";

    public static async Task InitializeAsync(ApplicationDbContext db)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        await EnsureRolesAsync(db);
        var departments = await EnsureDepartmentsAsync(db);
        var users = await EnsureUsersAsync(db, departments);
        var rooms = await EnsureRoomsAsync(db);
        await db.SaveChangesAsync();

        await EnsureMeetingsAsync(db, users, rooms);
        await transaction.CommitAsync();
    }

    private static async Task EnsureRolesAsync(ApplicationDbContext db)
    {
        var existingIds = await db.Roles.Select(x => x.RoleId).ToListAsync();
        var roles = new[]
        {
            new Role { RoleId = 1, RoleName = "Admin" },
            new Role { RoleId = 2, RoleName = "Organizer" },
            new Role { RoleId = 3, RoleName = "Employee" }
        };

        db.Roles.AddRange(roles.Where(x => !existingIds.Contains(x.RoleId)));
        await db.SaveChangesAsync();
    }

    private static async Task<Dictionary<string, Department>> EnsureDepartmentsAsync(ApplicationDbContext db)
    {
        string[] names =
        [
            "Công nghệ thông tin", "Nhân sự", "Kinh doanh", "Tài chính", "Marketing", "Vận hành"
        ];

        var existing = await db.Departments.ToDictionaryAsync(x => x.DepartmentName);
        foreach (var name in names)
        {
            if (!existing.ContainsKey(name))
            {
                var department = new Department { DepartmentName = name };
                db.Departments.Add(department);
                existing.Add(name, department);
            }
        }

        await db.SaveChangesAsync();
        return existing;
    }

    private static async Task<List<User>> EnsureUsersAsync(
        ApplicationDbContext db, IReadOnlyDictionary<string, Department> departments)
    {
        var users = await db.Users.ToListAsync();
        var byEmail = users.ToDictionary(x => x.Email, StringComparer.OrdinalIgnoreCase);
        var hasher = new PasswordHasher<User>();

        var definitions = new List<(string Name, string Email, string Phone, int RoleId, string Department)>
        {
            ("Quản trị viên", "admin@company.test", "0900000001", 1, "Công nghệ thông tin"),
            ("Nguyễn Văn Trường", "organizer@company.test", "0900000002", 2, "Công nghệ thông tin"),
            ("Lê Xuân Toàn", "employee@company.test", "0900000003", 3, "Công nghệ thông tin"),
            ("Đinh Ngọc Hiếu", "hieu@company.test", "0900000004", 3, "Nhân sự"),
            ("Nguyễn Quang Chiến", "chien@company.test", "0900000005", 3, "Kinh doanh"),
            ("Phạm Minh Anh", "minhanh@company.test", "0900000011", 2, "Kinh doanh"),
            ("Trần Thu Hà", "thuha@company.test", "0900000012", 2, "Nhân sự"),
            ("Võ Đức Minh", "ducminh@company.test", "0900000013", 2, "Vận hành"),
            ("Nguyễn Ngọc Lan", "ngoclan@company.test", "0900000014", 2, "Marketing"),
            ("Bùi Hoàng Nam", "hoangnam@company.test", "0900000021", 3, "Tài chính"),
            ("Đỗ Phương Linh", "phuonglinh@company.test", "0900000022", 3, "Marketing"),
            ("Lý Quốc Bảo", "quocbao@company.test", "0900000023", 3, "Vận hành"),
            ("Phan Thảo My", "thaomy@company.test", "0900000024", 3, "Nhân sự"),
            ("Hoàng Gia Huy", "giahuy@company.test", "0900000025", 3, "Công nghệ thông tin"),
            ("Đặng Khánh Vy", "khanhvy@company.test", "0900000026", 3, "Kinh doanh"),
            ("Ngô Thành Đạt", "thanhdat@company.test", "0900000027", 3, "Marketing"),
            ("Mai Thanh Trúc", "thanhtruc@company.test", "0900000028", 3, "Tài chính"),
            ("Vũ Nhật Quang", "nhatquang@company.test", "0900000029", 3, "Vận hành")
        };

        foreach (var definition in definitions)
        {
            if (byEmail.ContainsKey(definition.Email)) continue;

            var user = new User
            {
                FullName = definition.Name,
                Email = definition.Email,
                Phone = definition.Phone,
                RoleId = definition.RoleId,
                DepartmentId = departments[definition.Department].DepartmentId
            };
            user.PasswordHash = hasher.HashPassword(user, DemoPassword);
            db.Users.Add(user);
            byEmail.Add(user.Email, user);
        }

        await db.SaveChangesAsync();
        return byEmail.Values.ToList();
    }

    private static async Task<List<Room>> EnsureRoomsAsync(ApplicationDbContext db)
    {
        var existing = await db.Rooms.ToDictionaryAsync(x => x.RoomName);
        var definitions = new[]
        {
            new Room { RoomName = "Phòng Sakura", Location = "Tầng 2 · Khu A", Capacity = 8, Equipment = "Màn hình, bảng viết", Status = RoomStatus.Active },
            new Room { RoomName = "Phòng Orion", Location = "Tầng 3 · Khu A", Capacity = 16, Equipment = "Máy chiếu, camera hội nghị", Status = RoomStatus.Active },
            new Room { RoomName = "Phòng Summit", Location = "Tầng 4 · Khu B", Capacity = 30, Equipment = "Màn hình LED, camera, micro", Status = RoomStatus.Active },
            new Room { RoomName = "Phòng Cedar", Location = "Tầng 1 · Khu B", Capacity = 10, Equipment = "TV trình chiếu, bảng trắng", Status = RoomStatus.Active },
            new Room { RoomName = "Phòng Aurora", Location = "Tầng 5 · Khu A", Capacity = 20, Equipment = "Máy chiếu, loa, camera", Status = RoomStatus.Active },
            new Room { RoomName = "Phòng Polaris", Location = "Tầng 2 · Khu C", Capacity = 6, Equipment = "Màn hình", Status = RoomStatus.Maintenance },
            new Room { RoomName = "Phòng Focus", Location = "Tầng 3 · Khu C", Capacity = 4, Equipment = "Màn hình, bảng viết", Status = RoomStatus.Active }
        };

        foreach (var room in definitions)
        {
            if (!existing.ContainsKey(room.RoomName)) db.Rooms.Add(room);
        }

        await db.SaveChangesAsync();
        return await db.Rooms.AsNoTracking()
            .Where(x => !x.IsDisabled && x.Status == RoomStatus.Active)
            .OrderBy(x => x.RoomName)
            .ToListAsync();
    }

    private static async Task EnsureMeetingsAsync(ApplicationDbContext db, List<User> users, List<Room> rooms)
    {
        if (await db.Meetings.AnyAsync(x => x.Description != null && x.Description.StartsWith("[DEMO-SEED-V1]")))
            return;

        var organizerRoleIds = await db.Roles.Where(x => x.RoleName == "Organizer")
            .Select(x => x.RoleId).ToListAsync();
        var organizers = users.Where(x => organizerRoleIds.Contains(x.RoleId)).OrderBy(x => x.UserId).ToList();
        var attendees = users.Where(x => x.RoleId != 1).OrderBy(x => x.UserId).ToList();
        var adminId = users.First(x => x.Email == "admin@company.test").UserId;
        var existingKeys = await db.Meetings.Select(x => new { x.Title, x.StartTime }).ToListAsync();
        var existingSet = existingKeys.Select(x => (x.Title, x.StartTime)).ToHashSet();

        string[] topics =
        [
            "Kế hoạch tuần", "Cập nhật tiến độ dự án", "Rà soát ngân sách", "Đánh giá sprint",
            "Trao đổi nhân sự", "Kế hoạch khách hàng", "Tổng kết chiến dịch", "Đào tạo nội bộ",
            "Rà soát vận hành", "Lập kế hoạch quý"
        ];

        var now = DateTime.Now;
        var meetings = new List<Meeting>();
        var index = 0;
        for (var offset = -30; offset <= 27; offset += 3)
        {
            var date = DateTime.Today.AddDays(offset);
            for (var slot = 0; slot < 2; slot++)
            {
                var room = rooms[(index + slot * 2) % rooms.Count];
                var organizer = organizers[index % organizers.Count];
                var start = date.AddHours(slot == 0 ? 9 : 14);
                var title = $"{topics[index % topics.Length]} · {date:dd/MM} · {room.RoomName}";
                if (existingSet.Contains((title, start))) { index++; continue; }

                var cancelled = offset < 0 && index % 7 == 0;
                meetings.Add(new Meeting
                {
                    Title = title,
                    Description = $"[DEMO-SEED-V1] Cuộc họp mẫu thuộc phòng ban {organizer.DepartmentId}; dữ liệu phục vụ trình diễn lịch và báo cáo.",
                    RoomId = room.RoomId,
                    CreatedByUserId = organizer.UserId,
                    StartTime = start,
                    EndTime = start.AddMinutes(index % 3 == 0 ? 90 : 60),
                    CreatedAt = offset > 0
                        ? now.AddDays(-Math.Min(7, offset % 8 + 1))
                        : start.AddDays(-Math.Min(7, Math.Max(1, Math.Abs(offset)))),
                    Status = cancelled ? MeetingStatus.Cancelled : MeetingStatus.Confirmed,
                    CancelledAt = cancelled ? now : null,
                    CancelledByUserId = cancelled ? adminId : null,
                    CancelReason = cancelled ? "Cuộc họp mẫu đã được hủy." : null
                });
                index++;
            }
        }

        if (meetings.Count == 0) return;
        db.Meetings.AddRange(meetings);
        await db.SaveChangesAsync();

        for (var i = 0; i < meetings.Count; i++)
        {
            var meeting = meetings[i];
            var eligible = attendees.Where(x => x.UserId != meeting.CreatedByUserId).ToList();
            var count = Math.Min(5, eligible.Count);
            for (var n = 0; n < count; n++)
            {
                var user = eligible[(i * 3 + n) % eligible.Count];
                var status = (RsvpStatus)((i + n) % 3);
                db.MeetingParticipants.Add(new MeetingParticipant
                {
                    MeetingId = meeting.MeetingId,
                    UserId = user.UserId,
                    Status = status,
                    RespondedAt = status == RsvpStatus.Pending ? null : now.AddMinutes(-i - n)
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
