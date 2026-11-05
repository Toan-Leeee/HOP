using MeetingScheduler.Web.Data;
using MeetingScheduler.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MeetingScheduler.Web.Services;

/// <summary>Identity store adapter for the existing meeting scheduler User table.</summary>
public sealed class MeetingUserStore(ApplicationDbContext db) : IUserPasswordStore<User>, IUserEmailStore<User>
{
    public void Dispose() { }

    public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture));
    public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.Email);
    public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken) { user.Email = Normalize(userName); return Task.CompletedTask; }
    public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.Email.ToUpperInvariant());
    public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
    {
        db.Users.Add(user);
        return await SaveAsync(cancellationToken);
    }

    public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        db.Entry(user).State = EntityState.Modified;
        return await SaveAsync(cancellationToken);
    }

    public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
    {
        user.IsDisabled = true;
        db.Entry(user).State = EntityState.Modified;
        return await SaveAsync(cancellationToken);
    }

    public Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
        int.TryParse(userId, out var id)
            ? db.Users.Include(u => u.Role).Include(u => u.Department).FirstOrDefaultAsync(u => u.UserId == id, cancellationToken)
            : Task.FromResult<User?>(null);

    public Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
        FindByEmailAsync(normalizedUserName, cancellationToken);

    public Task SetPasswordHashAsync(User user, string? passwordHash, CancellationToken cancellationToken)
    { user.PasswordHash = passwordHash ?? ""; return Task.CompletedTask; }
    public Task<string?> GetPasswordHashAsync(User user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.PasswordHash);
    public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken) => Task.FromResult(!string.IsNullOrWhiteSpace(user.PasswordHash));

    public Task SetEmailAsync(User user, string? email, CancellationToken cancellationToken) { user.Email = Normalize(email); return Task.CompletedTask; }
    public Task<string?> GetEmailAsync(User user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.Email);
    public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken) => Task.FromResult(true);
    public Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var normalized = normalizedEmail.Trim().ToUpperInvariant();
        return db.Users.Include(u => u.Role).Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Email.ToUpper() == normalized, cancellationToken);
    }
    public Task<string?> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.Email.ToUpperInvariant());
    public Task SetNormalizedEmailAsync(User user, string? normalizedEmail, CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task<IdentityResult> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return IdentityResult.Success;
        }
        catch (DbUpdateException)
        {
            return IdentityResult.Failed(new IdentityError { Code = "DuplicateUserName", Description = "Email đã được sử dụng." });
        }
    }

    private static string Normalize(string? value) => value?.Trim().ToLowerInvariant() ?? "";
}
