using System.Data;
using MeetingScheduler.Web.Data;
using MeetingScheduler.Web.Models;
using MeetingScheduler.Web.Models.Enums;
using MeetingScheduler.Web.Services.Email;
using MeetingScheduler.Web.Services.Interfaces;
using MeetingScheduler.Web.ViewModels.Common;
using MeetingScheduler.Web.ViewModels.Meetings;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace MeetingScheduler.Web.Services;
public class MeetingService(ApplicationDbContext db, IEmailQueue emailQueue, ILogger<MeetingService> logger) : IMeetingService
{
    private static readonly System.Linq.Expressions.Expression<Func<Meeting, MeetingDetailsViewModel>> Map = m => new MeetingDetailsViewModel
    {
        MeetingId = m.MeetingId, Title = m.Title, Description = m.Description, RoomId = m.RoomId,
        StartTime = m.StartTime, EndTime = m.EndTime, RowVersion = m.RowVersion, RoomName = m.Room.RoomName,
        CreatedByUserId = m.CreatedByUserId, OrganizerName = m.CreatedByUser.FullName, Status = m.Status,
        CancelReason = m.CancelReason, CancelledAt = m.CancelledAt, ParticipantCount = m.Participants.Count,
        ParticipantIds = m.Participants.Select(p => p.UserId).ToList()
    };
    private Task<User?> ActorAsync(int id, CancellationToken ct) => db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id && !u.IsDisabled && !u.Department.IsDisabled, ct);
    public async Task<PagedResult<MeetingDetailsViewModel>> QueryAsync(MeetingFilter f, int actorUserId, CancellationToken ct = default)
    {
        var actor = await ActorAsync(actorUserId, ct);
        if (actor == null) return new() { Page = 1, PageSize = 10 };
        var q = db.Meetings.AsNoTracking();
        if (actor.RoleId != 1) q = q.Where(m => m.CreatedByUserId == actorUserId || m.Participants.Any(p => p.UserId == actorUserId));
        if (!string.IsNullOrWhiteSpace(f.SearchString)) q = q.Where(m => m.Title.Contains(f.SearchString.Trim()));
        if (f.RoomId.HasValue) q = q.Where(m => m.RoomId == f.RoomId);
        if (f.CreatedByUserId.HasValue) q = q.Where(m => m.CreatedByUserId == f.CreatedByUserId);
        if (f.Status.HasValue) q = q.Where(m => m.Status == f.Status);
        if (f.From.HasValue) q = q.Where(m => m.EndTime > f.From);
        if (f.To.HasValue) q = q.Where(m => m.StartTime < f.To);
        q = f.SortOrder switch { "start" => q.OrderBy(m => m.StartTime).ThenBy(m => m.MeetingId), "title" => q.OrderBy(m => m.Title).ThenBy(m => m.MeetingId), "title_desc" => q.OrderByDescending(m => m.Title).ThenBy(m => m.MeetingId), _ => q.OrderByDescending(m => m.StartTime).ThenBy(m => m.MeetingId) };
        return await ServiceSupport.PageAsync(q.Select(Map), f, ct);
    }
    public async Task<MeetingDetailsViewModel?> GetAsync(int id, int actorUserId, CancellationToken ct = default)
    {
        var actor = await ActorAsync(actorUserId, ct); if (actor == null) return null;
        return await db.Meetings.AsNoTracking().Where(m => m.MeetingId == id && (actor.RoleId == 1 || m.CreatedByUserId == actorUserId || m.Participants.Any(p => p.UserId == actorUserId))).Select(Map).FirstOrDefaultAsync(ct);
    }
    public async Task<ServiceResult> SaveAsync(MeetingEditViewModel m, int actorUserId, CancellationToken ct = default)
    {
        var error = ServiceSupport.Validate(m); if (error != null) return ServiceResult.Failure(error);
        if (string.IsNullOrWhiteSpace(m.Title)) return ServiceResult.Failure("Vui lòng nhập tiêu đề cuộc họp.");
        if (m.StartTime <= DateTime.Now) return ServiceResult.Failure("Thời gian bắt đầu phải ở trong tương lai.");
        // A range lock covers both the overlap read and the insertion/update.
        var result = await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            try
            {
                var actor = await ActorAsync(actorUserId, ct);
                if (actor == null || actor.RoleId is not (1 or 2)) return ServiceResult.Failure("Chỉ quản trị viên và người tổ chức mới được đặt lịch họp.");
                var room = await db.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.RoomId == m.RoomId && !r.IsDisabled && r.Status == RoomStatus.Active, ct);
                if (room == null) return ServiceResult.Failure("Vui lòng chọn phòng họp đang hoạt động.");
                var ids = m.ParticipantIds.Distinct().ToList();
                if (ids.Any(id => id <= 0)) return ServiceResult.Failure("Người tham dự không hợp lệ.");
                var organizerId = actorUserId;
                Meeting meeting;
                if (m.MeetingId == 0)
                {
                    meeting = new() { CreatedByUserId = actorUserId, CreatedAt = DateTime.UtcNow, Status = MeetingStatus.Confirmed };
                }
                else
                {
                    var existing = await db.Meetings.Include(x => x.Participants).FirstOrDefaultAsync(x => x.MeetingId == m.MeetingId, ct);
                    if (existing == null) return ServiceResult.Failure("Không tìm thấy cuộc họp.");
                    if (actor.RoleId != 1 && existing.CreatedByUserId != actorUserId) return ServiceResult.Failure("Bạn chỉ được sửa cuộc họp do mình tổ chức.");
                    if (existing.Status == MeetingStatus.Cancelled || existing.StartTime <= DateTime.Now) return ServiceResult.Failure("Không thể sửa cuộc họp đã hủy hoặc đã bắt đầu.");
                    if (m.RowVersion.Length == 0 || !existing.RowVersion.SequenceEqual(m.RowVersion)) return ServiceResult.Failure("Cuộc họp đã thay đổi. Hãy tải lại trang trước khi sửa.");
                    db.Entry(existing).Property(x => x.RowVersion).OriginalValue = m.RowVersion;
                    meeting = existing; organizerId = existing.CreatedByUserId;
                }
                if (ids.Append(organizerId).Distinct().Count() > room.Capacity) return ServiceResult.Failure("Phòng họp không đủ sức chứa cho người tham dự và người tổ chức.");
                if (await db.Users.AsNoTracking().CountAsync(u => ids.Contains(u.UserId) && !u.IsDisabled && !u.Department.IsDisabled, ct) != ids.Count) return ServiceResult.Failure("Mọi người tham dự phải có tài khoản và phòng ban đang hoạt động.");
                if (await db.Meetings.AsNoTracking().AnyAsync(x => x.RoomId == m.RoomId && x.MeetingId != m.MeetingId && x.Status == MeetingStatus.Confirmed && x.StartTime < m.EndTime && m.StartTime < x.EndTime, ct)) return ServiceResult.Failure("Phòng họp đã được đặt trong khoảng thời gian này.");
                var scheduleChanged = meeting.StartTime != m.StartTime || meeting.EndTime != m.EndTime || meeting.RoomId != m.RoomId;
                meeting.Title = m.Title.Trim(); meeting.Description = m.Description; meeting.StartTime = m.StartTime; meeting.EndTime = m.EndTime; meeting.RoomId = m.RoomId;
                if (m.MeetingId != 0) meeting.UpdatedAt = DateTime.UtcNow;
                foreach (var p in meeting.Participants.Where(p => !ids.Contains(p.UserId)).ToList()) db.MeetingParticipants.Remove(p);
                foreach (var id in ids)
                {
                    var participant = meeting.Participants.FirstOrDefault(p => p.UserId == id);
                    if (participant == null) meeting.Participants.Add(new() { UserId = id, Status = RsvpStatus.Pending });
                    else if (scheduleChanged) { participant.Status = RsvpStatus.Pending; participant.RespondedAt = null; }
                }
                if (m.MeetingId == 0) db.Meetings.Add(meeting);
                await db.SaveChangesAsync(ct);
                db.AuditLogs.Add(ServiceSupport.Audit(actorUserId, m.MeetingId == 0 ? "Create" : "Update", "Meeting", meeting.MeetingId));
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return ServiceResult.Success(meeting.MeetingId);
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync(ct); db.ChangeTracker.Clear();
                return ServiceResult.Failure("Cuộc họp đã thay đổi. Hãy tải lại trang và thử lại.");
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync(ct); db.ChangeTracker.Clear();
                return ServiceResult.Failure("Lịch đặt phòng xung đột với một cập nhật khác. Hãy tải lại trang và thử lại.");
            }
            catch (SqlException ex) when (ex.Number is 1205 or 1222)
            {
                await tx.RollbackAsync(ct); db.ChangeTracker.Clear();
                return ServiceResult.Failure("Một yêu cầu đặt phòng khác đang được xử lý. Vui lòng thử lại.");
            }
        });
        if (result.Succeeded) await QueueNotificationsAsync(result.EntityId!.Value, "Cập nhật lời mời họp", ct);
        return result;
    }
    public async Task<ServiceResult> CancelAsync(CancelMeetingViewModel m, int actorUserId, CancellationToken ct = default)
    {
        var error = ServiceSupport.Validate(m); if (error != null) return ServiceResult.Failure(error);
        if (string.IsNullOrWhiteSpace(m.CancelReason)) return ServiceResult.Failure("Vui lòng nhập lý do hủy.");
        var result = await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            try
            {
                var actor = await ActorAsync(actorUserId, ct);
                if (actor == null || actor.RoleId is not (1 or 2)) return ServiceResult.Failure("Bạn cần quyền người tổ chức hoặc quản trị viên.");
                var meeting = await db.Meetings.FirstOrDefaultAsync(x => x.MeetingId == m.MeetingId, ct);
                if (meeting == null) return ServiceResult.Failure("Không tìm thấy cuộc họp.");
                if (actor.RoleId != 1 && meeting.CreatedByUserId != actorUserId) return ServiceResult.Failure("Bạn chỉ được hủy cuộc họp do mình tổ chức.");
                if (meeting.Status == MeetingStatus.Cancelled) return ServiceResult.Failure("Cuộc họp đã được hủy.");
                if (meeting.StartTime <= DateTime.Now) return ServiceResult.Failure("Không thể hủy cuộc họp đã bắt đầu.");
                if (m.RowVersion.Length == 0 || !meeting.RowVersion.SequenceEqual(m.RowVersion)) return ServiceResult.Failure("Cuộc họp đã thay đổi. Hãy tải lại trang trước khi hủy.");
                db.Entry(meeting).Property(x => x.RowVersion).OriginalValue = m.RowVersion;
                meeting.Status = MeetingStatus.Cancelled; meeting.CancelledAt = DateTime.UtcNow; meeting.CancelledByUserId = actorUserId; meeting.CancelReason = m.CancelReason.Trim(); meeting.UpdatedAt = DateTime.UtcNow;
                db.AuditLogs.Add(ServiceSupport.Audit(actorUserId, "Cancel", "Meeting", meeting.MeetingId, meeting.CancelReason));
                await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
                return ServiceResult.Success(meeting.MeetingId);
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
        if (result.Succeeded) await QueueNotificationsAsync(m.MeetingId, "Cuộc họp đã hủy", ct);
        return result;
    }
    private async Task QueueNotificationsAsync(int meetingId, string subject, CancellationToken ct)
    {
        try
        {
            var meeting = await db.Meetings.AsNoTracking().Where(m => m.MeetingId == meetingId).Select(m => new { m.Title, m.StartTime, m.EndTime, m.CancelReason }).SingleAsync(ct);
            var recipients = await db.MeetingParticipants.AsNoTracking().Where(p => p.MeetingId == meetingId && !p.User.IsDisabled && !p.User.Department.IsDisabled).Select(p => p.User.Email).ToListAsync(ct);
            foreach (var address in recipients)
                if (!emailQueue.TryEnqueue(new(address, subject + ": " + meeting.Title, $"{meeting.Title}\n{meeting.StartTime:g} - {meeting.EndTime:g}\n{meeting.CancelReason}")))
                    logger.LogWarning("Email notification was not queued for meeting {MeetingId} (SMTP unconfigured or queue full).", meetingId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Meeting {MeetingId} was saved, but optional notifications could not be queued.", meetingId);
        }
    }
}
