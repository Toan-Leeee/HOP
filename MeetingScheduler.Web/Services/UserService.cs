using MeetingScheduler.Web.Data;
using MeetingScheduler.Web.Models;
using MeetingScheduler.Web.Services.Interfaces;
using MeetingScheduler.Web.ViewModels.Common;
using MeetingScheduler.Web.ViewModels.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace MeetingScheduler.Web.Services;
public class UserService(ApplicationDbContext db, IPasswordHasher<User> hasher) : IUserService
{
    private static readonly System.Linq.Expressions.Expression<Func<User, UserEditViewModel>> Map = u => new UserEditViewModel { UserId = u.UserId, FullName = u.FullName, Email = u.Email, Phone = u.Phone, DepartmentId = u.DepartmentId, RoleId = u.RoleId, IsDisabled = u.IsDisabled, DepartmentName = u.Department.DepartmentName, RoleName = u.Role.RoleName };
    public Task<PagedResult<UserEditViewModel>> QueryAsync(UserFilter f, CancellationToken ct = default)
    {
        var q = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(f.SearchString)) q = q.Where(u => u.FullName.Contains(f.SearchString.Trim()) || u.Email.Contains(f.SearchString.Trim()));
        if (f.DepartmentId.HasValue) q = q.Where(u => u.DepartmentId == f.DepartmentId);
        if (f.RoleId.HasValue) q = q.Where(u => u.RoleId == f.RoleId);
        if (f.IsDisabled.HasValue) q = q.Where(u => u.IsDisabled == f.IsDisabled);
        q = f.SortOrder switch { "email" => q.OrderBy(u => u.Email).ThenBy(u => u.UserId), "email_desc" => q.OrderByDescending(u => u.Email).ThenBy(u => u.UserId), "name_desc" => q.OrderByDescending(u => u.FullName).ThenBy(u => u.UserId), _ => q.OrderBy(u => u.FullName).ThenBy(u => u.UserId) };
        return ServiceSupport.PageAsync(q.Select(Map), f, ct);
    }
    public Task<UserEditViewModel?> GetAsync(int id, CancellationToken ct = default) => db.Users.AsNoTracking().Where(u => u.UserId == id).Select(Map).FirstOrDefaultAsync(ct);
    public Task<List<LookupItem>> GetLookupAsync(CancellationToken ct = default) => db.Users.AsNoTracking().Where(u => !u.IsDisabled && !u.Department.IsDisabled).OrderBy(u => u.FullName).Select(u => new LookupItem(u.UserId, u.FullName + " (" + u.Email + ")")).ToListAsync(ct);
    public Task<List<LookupItem>> GetRolesAsync(CancellationToken ct = default) => db.Roles.AsNoTracking().OrderBy(r => r.RoleId).Select(r => new LookupItem(r.RoleId, r.RoleName)).ToListAsync(ct);
    private async Task<string?> ValidateAsync(UserEditViewModel m, CancellationToken ct)
    {
        var error = ServiceSupport.Validate(m); if (error != null) return error;
        if (string.IsNullOrWhiteSpace(m.FullName)) return "Vui lòng nhập họ tên.";
        if (!await db.Departments.AsNoTracking().AnyAsync(d => d.DepartmentId == m.DepartmentId && !d.IsDisabled, ct)) return "Vui lòng chọn phòng ban đang hoạt động.";
        if (!await db.Roles.AsNoTracking().AnyAsync(r => r.RoleId == m.RoleId, ct)) return "Không tìm thấy vai trò.";
        var email = m.Email.Trim().ToLowerInvariant();
        if (await db.Users.AsNoTracking().AnyAsync(u => u.Email == email && u.UserId != m.UserId, ct)) return "Email đã được sử dụng.";
        return null;
    }
    public async Task<ServiceResult> CreateAsync(UserCreateViewModel m, int actorUserId, CancellationToken ct = default)
    {
        if (!await ServiceSupport.IsAdminAsync(db, actorUserId, ct)) return ServiceResult.Failure("Bạn cần quyền quản trị viên.");
        if (m.UserId != 0) return ServiceResult.Failure("Mã tài khoản mới không hợp lệ.");
        var error = await ValidateAsync(m, ct); if (error != null) return ServiceResult.Failure(error);
        var user = new User { FullName = m.FullName.Trim(), Email = m.Email.Trim().ToLowerInvariant(), Phone = m.Phone?.Trim(), DepartmentId = m.DepartmentId, RoleId = m.RoleId, IsDisabled = m.IsDisabled };
        user.PasswordHash = hasher.HashPassword(user, m.Password);
        db.Users.Add(user);
        return await ServiceSupport.CommitAsync(db, actorUserId, "Create", "User", () => user.UserId, ct);
    }
    public async Task<ServiceResult> UpdateAsync(UserEditViewModel m, int actorUserId, CancellationToken ct = default)
    {
        if (!await ServiceSupport.IsAdminAsync(db, actorUserId, ct)) return ServiceResult.Failure("Bạn cần quyền quản trị viên.");
        var error = await ValidateAsync(m, ct); if (error != null) return ServiceResult.Failure(error);
        if (m.UserId == actorUserId && (m.IsDisabled || m.RoleId != 1)) return ServiceResult.Failure("Bạn không thể khóa hoặc hạ quyền chính tài khoản quản trị đang sử dụng.");
        var user = await db.Users.FindAsync([m.UserId], ct); if (user == null) return ServiceResult.Failure("Không tìm thấy tài khoản.");
        user.FullName = m.FullName.Trim(); user.Email = m.Email.Trim().ToLowerInvariant(); user.Phone = m.Phone?.Trim(); user.DepartmentId = m.DepartmentId; user.RoleId = m.RoleId; user.IsDisabled = m.IsDisabled;
        return await ServiceSupport.CommitAsync(db, actorUserId, "Update", "User", () => user.UserId, ct);
    }
    public async Task<ServiceResult> DisableAsync(int id, int actorUserId, CancellationToken ct = default)
    {
        if (!await ServiceSupport.IsAdminAsync(db, actorUserId, ct)) return ServiceResult.Failure("Bạn cần quyền quản trị viên.");
        if (id == actorUserId) return ServiceResult.Failure("Bạn không thể khóa tài khoản của chính mình.");
        var user = await db.Users.FindAsync([id], ct); if (user == null) return ServiceResult.Failure("Không tìm thấy tài khoản.");
        user.IsDisabled = true;
        return await ServiceSupport.CommitAsync(db, actorUserId, "Disable", "User", () => id, ct);
    }
}
