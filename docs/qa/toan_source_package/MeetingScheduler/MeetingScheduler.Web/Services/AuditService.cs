using MeetingScheduler.Web.Data;
using MeetingScheduler.Web.Services.Interfaces;
namespace MeetingScheduler.Web.Services;
public class AuditService(ApplicationDbContext db) : IAuditService
{
    public async Task LogAsync(int userId, string action, string entityName, int entityId, string? details = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(action) || action.Length > 100 || string.IsNullOrWhiteSpace(entityName) || entityName.Length > 100) throw new ArgumentException("Audit action and entity name must contain 1-100 characters.");
        db.AuditLogs.Add(ServiceSupport.Audit(userId, action, entityName, entityId, details));
        await db.SaveChangesAsync(ct);
    }
}
