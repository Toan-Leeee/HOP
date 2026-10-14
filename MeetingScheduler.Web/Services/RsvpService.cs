using System.Data;
using MeetingScheduler.Web.Data;
using MeetingScheduler.Web.Models.Enums;
using MeetingScheduler.Web.Services.Interfaces;
using MeetingScheduler.Web.ViewModels.Common;
using MeetingScheduler.Web.ViewModels.Meetings;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace MeetingScheduler.Web.Services;
public class RsvpService(ApplicationDbContext db) : IRsvpService
{
    public async Task<PagedResult<RsvpTrackingViewModel>?> QueryAsync(RsvpFilter f, int actorUserId, CancellationToken ct = default)
    {
        var actor = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == actorUserId && !u.IsDisabled && !u.Department.IsDisabled, ct);
        if (actor == null || !await db.Meetings.AsNoTracking().AnyAsync(m => m.MeetingId == f.MeetingId && (actor.RoleId == 1 || m.CreatedByUserId == actorUserId), ct)) return null;
        var q = db.MeetingParticipants.AsNoTracking().Where(p => p.MeetingId == f.MeetingId);
        if (f.Status.HasValue) q = q.Where(p => p.Status == f.Status);
        if (!string.IsNullOrWhiteSpace(f.SearchString)) q = q.Where(p => p.User.FullName.Contains(f.SearchString.Trim()) || p.User.Email.Contains(f.SearchString.Trim()));
        q = f.SortOrder switch { "status" => q.OrderBy(p => p.Status).ThenBy(p => p.UserId), "status_desc" => q.OrderByDescending(p => p.Status).ThenBy(p => p.UserId), "responded" => q.OrderBy(p => p.RespondedAt).ThenBy(p => p.UserId), "responded_desc" => q.OrderByDescending(p => p.RespondedAt).ThenBy(p => p.UserId), "name_desc" => q.OrderByDescending(p => p.User.FullName).ThenBy(p => p.UserId), _ => q.OrderBy(p => p.User.FullName).ThenBy(p => p.UserId) };
        return await ServiceSupport.PageAsync(q.Select(p => new RsvpTrackingViewModel { MeetingId = p.MeetingId, UserId = p.UserId, FullName = p.User.FullName, Email = p.User.Email, Status = p.Status, RespondedAt = p.RespondedAt }), f, ct);
    }
    public async Task<ServiceResult> RespondAsync(int meetingId, int actorUserId, RsvpStatus status, CancellationToken ct = default)
    {
        if (status is not (RsvpStatus.Accepted or RsvpStatus.Declined)) return ServiceResult.Failure("Vui lòng chọn đồng ý hoặc từ chối tham dự.");
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            try
            {
                if (!await db.Users.AsNoTracking().AnyAsync(u => u.UserId == actorUserId && !u.IsDisabled && !u.Department.IsDisabled, ct)) return ServiceResult.Failure("Bạn cần tài khoản đang hoạt động.");
                var meeting = await db.Meetings.AsNoTracking().FirstOrDefaultAsync(m => m.MeetingId == meetingId, ct);
                if (meeting == null || meeting.Status != MeetingStatus.Confirmed || meeting.StartTime <= DateTime.Now) return ServiceResult.Failure("Cuộc họp không còn nhận phản hồi.");
                var participant = await db.MeetingParticipants.FirstOrDefaultAsync(p => p.MeetingId == meetingId && p.UserId == actorUserId, ct);
                if (participant == null) return ServiceResult.Failure("Bạn không có trong danh sách được mời tham dự.");
                participant.Status = status; participant.RespondedAt = DateTime.UtcNow;
                db.AuditLogs.Add(ServiceSupport.Audit(actorUserId, "Rsvp", "Meeting", meetingId, status.ToString()));
                await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
                return ServiceResult.Success(meetingId);
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync(ct); db.ChangeTracker.Clear();
                return ServiceResult.Failure("Cuộc họp đã thay đổi. Hãy tải lại trang và thử lại.");
            }
            catch (SqlException ex) when (ex.Number is 1205 or 1222)
            {
                await tx.RollbackAsync(ct); db.ChangeTracker.Clear();
                return ServiceResult.Failure("Một cập nhật cuộc họp khác đang được xử lý. Vui lòng thử lại.");
            }
        });
    }
}
