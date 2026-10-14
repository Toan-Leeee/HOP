using MeetingScheduler.Web.Data;
using MeetingScheduler.Web.Models.Enums;
using MeetingScheduler.Web.Services.Interfaces;
using MeetingScheduler.Web.ViewModels.Rooms;
using Microsoft.EntityFrameworkCore;
namespace MeetingScheduler.Web.Services;
public class RoomAvailabilityService(ApplicationDbContext db) : IRoomAvailabilityService
{
    public async Task<List<RoomViewModel>> GetAvailableAsync(DateTime start, DateTime end, int minimumCapacity = 1, int? excludeMeetingId = null, CancellationToken ct = default)
    {
        if (end <= start) return [];
        return await db.Rooms.AsNoTracking().Where(r => !r.IsDisabled && r.Status == RoomStatus.Active && r.Capacity >= Math.Max(1, minimumCapacity)
            && !r.Meetings.Any(m => m.Status == MeetingStatus.Confirmed && (!excludeMeetingId.HasValue || m.MeetingId != excludeMeetingId.Value) && m.StartTime < end && start < m.EndTime))
            .OrderBy(r => r.Capacity).ThenBy(r => r.RoomName).Select(r => new RoomViewModel { RoomId = r.RoomId, RoomName = r.RoomName, Capacity = r.Capacity, Status = r.Status, Location = r.Location, Equipment = r.Equipment }).ToListAsync(ct);
    }
}
