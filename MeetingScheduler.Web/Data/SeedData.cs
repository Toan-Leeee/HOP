using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MeetingScheduler.Web.Models;
using MeetingScheduler.Web.Models.Enums;
namespace MeetingScheduler.Web.Data;
public static class SeedData
{
    public static async Task InitializeAsync(ApplicationDbContext db)
    {
        if (await db.Roles.AnyAsync()) return;
        await using var transaction = await db.Database.BeginTransactionAsync();
        db.Roles.AddRange(new Role { RoleId = 1, RoleName = "Admin" }, new Role { RoleId = 2, RoleName = "Organizer" }, new Role { RoleId = 3, RoleName = "Employee" });
        var departments = new[] { new Department { DepartmentName = "Công nghệ thông tin" }, new Department { DepartmentName = "Nhân sự" }, new Department { DepartmentName = "Kinh doanh" } };
        db.Departments.AddRange(departments);
        await db.SaveChangesAsync();
        var hasher = new PasswordHasher<User>();
        var users = new[] {
            new User { FullName = "Quản trị viên", Email = "admin@company.test", RoleId = 1, DepartmentId = departments[0].DepartmentId },
            new User { FullName = "Nguyễn Văn Trường", Email = "organizer@company.test", RoleId = 2, DepartmentId = departments[0].DepartmentId },
            new User { FullName = "Lê Xuân Toàn", Email = "employee@company.test", RoleId = 3, DepartmentId = departments[0].DepartmentId },
            new User { FullName = "Đinh Ngọc Hiếu", Email = "hieu@company.test", RoleId = 3, DepartmentId = departments[1].DepartmentId },
            new User { FullName = "Nguyễn Quang Chiến", Email = "chien@company.test", RoleId = 3, DepartmentId = departments[2].DepartmentId }
        };
        foreach (var user in users) user.PasswordHash = hasher.HashPassword(user, "Demo@12345");
        db.Users.AddRange(users);
        var rooms = new[] {
            new Room { RoomName = "Phòng Lotus", Location = "Tầng 2", Capacity = 12, Equipment = "Màn hình, bảng viết", Status = RoomStatus.Active },
            new Room { RoomName = "Phòng Horizon", Location = "Tầng 3", Capacity = 24, Equipment = "Máy chiếu, camera", Status = RoomStatus.Active },
            new Room { RoomName = "Phòng Studio", Location = "Tầng 1", Capacity = 6, Equipment = "Màn hình", Status = RoomStatus.Maintenance }
        };
        db.Rooms.AddRange(rooms);
        await db.SaveChangesAsync();
        var day = DateTime.Today.AddDays(1);
        var meetings = new[] {
            new Meeting { Title = "Họp kế hoạch tuần", Description = "Thống nhất công việc và tiến độ triển khai.", RoomId = rooms[0].RoomId, CreatedByUserId = users[1].UserId, StartTime = day.AddHours(9), EndTime = day.AddHours(10), CreatedAt = DateTime.Now, Status = MeetingStatus.Confirmed },
            new Meeting { Title = "Đánh giá sprint", RoomId = rooms[1].RoomId, CreatedByUserId = users[1].UserId, StartTime = day.AddHours(14), EndTime = day.AddHours(15.5), CreatedAt = DateTime.Now, Status = MeetingStatus.Confirmed },
            new Meeting { Title = "Trao đổi nhân sự", RoomId = rooms[0].RoomId, CreatedByUserId = users[1].UserId, StartTime = day.AddDays(-7).AddHours(10), EndTime = day.AddDays(-7).AddHours(11), CreatedAt = DateTime.Now, Status = MeetingStatus.Cancelled, CancelledAt = DateTime.Now, CancelledByUserId = users[0].UserId, CancelReason = "Điều chỉnh kế hoạch" }
        };
        db.Meetings.AddRange(meetings);
        await db.SaveChangesAsync();
        foreach (var meeting in meetings)
            for (int i = 2; i < users.Length; i++) db.MeetingParticipants.Add(new MeetingParticipant { MeetingId = meeting.MeetingId, UserId = users[i].UserId, Status = (RsvpStatus)(i - 2), RespondedAt = i == 2 ? null : DateTime.Now });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
