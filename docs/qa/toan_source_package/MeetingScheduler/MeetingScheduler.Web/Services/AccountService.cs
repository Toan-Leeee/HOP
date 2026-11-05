using MeetingScheduler.Web.Data;
using MeetingScheduler.Web.Models;
using MeetingScheduler.Web.Services.Interfaces;
using MeetingScheduler.Web.ViewModels.Account;
using MeetingScheduler.Web.ViewModels.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace MeetingScheduler.Web.Services;
public class AccountService(ApplicationDbContext db, IPasswordHasher<User> hasher) : IAccountService
{
    public Task<bool> ValidateSessionAsync(int userId, string roleName, CancellationToken ct = default) =>
        db.Users.AsNoTracking().AnyAsync(u => u.UserId == userId && !u.IsDisabled && !u.Department.IsDisabled && u.Role.RoleName == roleName, ct);
    public async Task<AuthenticatedUser?> LoginAsync(LoginViewModel m, CancellationToken ct = default)
    {
        if (ServiceSupport.Validate(m) != null) return null;
        var email = m.Email.Trim().ToLowerInvariant();
        var user = await db.Users.Include(u => u.Role).Include(u => u.Department).FirstOrDefaultAsync(u => u.Email == email && !u.IsDisabled && !u.Department.IsDisabled, ct);
        if (user == null) return null;
        PasswordVerificationResult result;
        try { result = hasher.VerifyHashedPassword(user, user.PasswordHash, m.Password); }
        catch (FormatException) { return null; }
        if (result == PasswordVerificationResult.Failed) return null;
        if (result == PasswordVerificationResult.SuccessRehashNeeded) user.PasswordHash = hasher.HashPassword(user, m.Password);
        db.AuditLogs.Add(ServiceSupport.Audit(user.UserId, "Login", "User", user.UserId));
        await db.SaveChangesAsync(ct);
        return new(user.UserId, user.FullName, user.Email, user.Role.RoleName);
    }
    public Task<ProfileViewModel?> GetProfileAsync(int userId, CancellationToken ct = default) => db.Users.AsNoTracking().Where(u => u.UserId == userId && !u.IsDisabled && !u.Department.IsDisabled).Select(u => new ProfileViewModel { FullName = u.FullName, Email = u.Email, Phone = u.Phone }).FirstOrDefaultAsync(ct);
    public async Task<ServiceResult> UpdateProfileAsync(int userId, ProfileViewModel m, CancellationToken ct = default)
    {
        var error = ServiceSupport.Validate(m); if (error != null) return ServiceResult.Failure(error);
        if (string.IsNullOrWhiteSpace(m.FullName)) return ServiceResult.Failure("Vui lòng nhập họ tên.");
        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDisabled && !u.Department.IsDisabled, ct);
        if (user == null) return ServiceResult.Failure("Không tìm thấy tài khoản đang hoạt động.");
        var email = m.Email.Trim().ToLowerInvariant();
        if (await db.Users.AsNoTracking().AnyAsync(u => u.Email == email && u.UserId != userId, ct)) return ServiceResult.Failure("Email đã được sử dụng.");
        user.FullName = m.FullName.Trim(); user.Email = email; user.Phone = m.Phone?.Trim();
        return await ServiceSupport.CommitAsync(db, userId, "UpdateProfile", "User", () => userId, ct);
    }
    public async Task<ServiceResult> ChangePasswordAsync(int userId, ChangePasswordViewModel m, CancellationToken ct = default)
    {
        var error = ServiceSupport.Validate(m); if (error != null) return ServiceResult.Failure(error);
        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDisabled && !u.Department.IsDisabled, ct);
        if (user == null) return ServiceResult.Failure("Không tìm thấy tài khoản đang hoạt động.");
        PasswordVerificationResult result;
        try { result = hasher.VerifyHashedPassword(user, user.PasswordHash, m.CurrentPassword); }
        catch (FormatException) { return ServiceResult.Failure("Mật khẩu hiện tại không đúng."); }
        if (result == PasswordVerificationResult.Failed) return ServiceResult.Failure("Mật khẩu hiện tại không đúng.");
        user.PasswordHash = hasher.HashPassword(user, m.NewPassword);
        return await ServiceSupport.CommitAsync(db, userId, "ChangePassword", "User", () => userId, ct);
    }
}
