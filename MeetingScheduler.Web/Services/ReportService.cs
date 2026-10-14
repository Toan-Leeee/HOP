using MeetingScheduler.Web.Data;
using MeetingScheduler.Web.Models.Enums;
using MeetingScheduler.Web.Services.Interfaces;
using MeetingScheduler.Web.ViewModels.Reports;
using Microsoft.EntityFrameworkCore;
namespace MeetingScheduler.Web.Services;
public class ReportService(ApplicationDbContext db) : IReportService
{
    public async Task<ReportResult> GetAsync(ReportFilter f, CancellationToken ct = default)
    {
        if (f.To <= f.From) throw new ArgumentException("Ngày kết thúc báo cáo phải sau ngày bắt đầu.");
        var q = db.Meetings.AsNoTracking().Where(m => m.StartTime < f.To && m.EndTime > f.From);
        if (f.RoomId.HasValue) q = q.Where(m => m.RoomId == f.RoomId);
        var rows = await q.OrderBy(m => m.StartTime).Select(m => new { m.MeetingId, m.RoomId, m.Room.RoomName, m.Status, m.StartTime, m.EndTime }).ToListAsync(ct);
        var responses = await db.MeetingParticipants.AsNoTracking().Where(p => q.Any(m => m.MeetingId == p.MeetingId && m.Status == MeetingStatus.Confirmed)).GroupBy(p => p.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync(ct);
        return new()
        {
            Filter = f, TotalMeetings = rows.Count, CancelledMeetings = rows.Count(m => m.Status == MeetingStatus.Cancelled),
            AcceptedResponses = responses.Where(r => r.Status == RsvpStatus.Accepted).Sum(r => r.Count),
            PendingResponses = responses.Where(r => r.Status == RsvpStatus.Pending).Sum(r => r.Count),
            DeclinedResponses = responses.Where(r => r.Status == RsvpStatus.Declined).Sum(r => r.Count),
            Rooms = rows.Where(m => m.Status == MeetingStatus.Confirmed).GroupBy(m => new { m.RoomId, m.RoomName }).Select(g => new RoomReport(g.Key.RoomId, g.Key.RoomName, g.Count(), g.Sum(m => ((m.EndTime < f.To ? m.EndTime : f.To) - (m.StartTime > f.From ? m.StartTime : f.From)).TotalHours))).OrderByDescending(r => r.TotalHours).ToList()
        };
    }
}
