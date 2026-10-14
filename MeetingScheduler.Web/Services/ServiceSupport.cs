using System.ComponentModel.DataAnnotations;
using MeetingScheduler.Web.Data;
using MeetingScheduler.Web.Models;
using MeetingScheduler.Web.ViewModels.Common;
using Microsoft.EntityFrameworkCore;
namespace MeetingScheduler.Web.Services;
internal static class ServiceSupport
{
    public static string? Validate(object model)
    {
        var errors = new List<ValidationResult>();
        return Validator.TryValidateObject(model, new ValidationContext(model), errors, true)
            ? null : string.Join(" ", errors.Select(e => e.ErrorMessage));
    }
    public static Task<bool> IsAdminAsync(ApplicationDbContext db, int id, CancellationToken ct) =>
        db.Users.AsNoTracking().AnyAsync(u => u.UserId == id && !u.IsDisabled && !u.Department.IsDisabled && u.RoleId == 1, ct);
    public static async Task<PagedResult<T>> PageAsync<T>(IQueryable<T> ordered, QueryFilter filter, CancellationToken ct)
    {
        var page = Math.Clamp(filter.Page, 1, 1000000);
        var size = Math.Clamp(filter.PageSize <= 0 ? 10 : filter.PageSize, 1, 100);
        var count = await ordered.CountAsync(ct);
        return new() { Items = await ordered.Skip((page - 1) * size).Take(size).ToListAsync(ct), TotalCount = count, Page = page, PageSize = size };
    }
    public static AuditLog Audit(int actor, string action, string entity, int id, string? details = null) =>
        new() { UserId = actor, Action = action, EntityName = entity, EntityId = id, Details = details, CreatedAt = DateTime.UtcNow };
    public static async Task<ServiceResult> CommitAsync(ApplicationDbContext db, int actor, string action, string entity, Func<int> id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.SaveChangesAsync(ct);
            db.AuditLogs.Add(Audit(actor, action, entity, id()));
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return ServiceResult.Success(id());
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            return ServiceResult.Failure("Bản ghi xung đột với dữ liệu hiện có. Hãy tải lại trang và thử lại.");
        }
    }
}
