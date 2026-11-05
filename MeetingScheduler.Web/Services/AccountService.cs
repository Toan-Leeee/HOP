using MeetingScheduler.Web.Data;
using MeetingScheduler.Web.Models;
using MeetingScheduler.Web.Services.Interfaces;
using MeetingScheduler.Web.ViewModels.Account;
using MeetingScheduler.Web.ViewModels.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MeetingScheduler.Web.Services.Email;
using MeetingScheduler.Web.ViewModels.Email;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.Text;
namespace MeetingScheduler.Web.Services;
public class AccountService(ApplicationDbContext db, IPasswordHasher<User> hasher, IEmailQueue emailQueue) : IAccountService
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
    public async Task RequestPasswordResetAsync(string email, string resetUrl, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalized && !u.IsDisabled && !u.Department.IsDisabled, ct);
        if (user == null) return;
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        user.PasswordResetTokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        user.PasswordResetExpiresAt = DateTime.UtcNow.AddHours(1);
        await db.SaveChangesAsync(ct);
        var separator = resetUrl.Contains('?') ? "&" : "?";
        var link = resetUrl + separator + "token=" + Uri.EscapeDataString(token);
        await emailQueue.EnqueueAsync(new(user.Email, "Đặt lại mật khẩu", $"Mở liên kết sau trong vòng 1 giờ để đặt lại mật khẩu:\n{link}"), ct: ct);
    }
    public async Task<bool> ResetPasswordAsync(ResetPasswordViewModel m, CancellationToken ct = default)
    {
        if (ServiceSupport.Validate(m) != null) return false;
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == m.Email.Trim().ToLowerInvariant() && !u.IsDisabled && !u.Department.IsDisabled, ct);
        if (user?.PasswordResetTokenHash == null || user.PasswordResetExpiresAt <= DateTime.UtcNow) return false;
        var supplied = SHA256.HashData(Encoding.UTF8.GetBytes(m.Token));
        byte[] expected;
        try { expected = Convert.FromBase64String(user.PasswordResetTokenHash); } catch (FormatException) { return false; }
        if (!CryptographicOperations.FixedTimeEquals(supplied, expected)) return false;
        user.PasswordHash = hasher.HashPassword(user, m.NewPassword);
        user.PasswordResetTokenHash = null; user.PasswordResetExpiresAt = null;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
