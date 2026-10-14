using MeetingScheduler.Web.Data;
using MeetingScheduler.Web.Infrastructure;
using MeetingScheduler.Web.Models;
using MeetingScheduler.Web.Models.Enums;
using MeetingScheduler.Web.Services.Interfaces;
using MeetingScheduler.Web.ViewModels.Common;
using MeetingScheduler.Web.ViewModels.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
namespace MeetingScheduler.Web.Services;
public class RoomService(ApplicationDbContext db, IMemoryCache cache) : IRoomService
{
    private static readonly System.Linq.Expressions.Expression<Func<Room, RoomViewModel>> Map = r => new RoomViewModel { RoomId = r.RoomId, RoomName = r.RoomName, Capacity = r.Capacity, Location = r.Location, Equipment = r.Equipment, Status = r.Status, IsDisabled = r.IsDisabled };
    public Task<PagedResult<RoomViewModel>> QueryAsync(RoomFilter f, CancellationToken ct = default)
    {
        var q = db.Rooms.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(f.SearchString)) q = q.Where(r => r.RoomName.Contains(f.SearchString.Trim()));
        if (f.Status.HasValue) q = q.Where(r => r.Status == f.Status);
        if (f.MinimumCapacity.HasValue) q = q.Where(r => r.Capacity >= f.MinimumCapacity);
        if (f.IsDisabled.HasValue) q = q.Where(r => r.IsDisabled == f.IsDisabled);
        q = f.SortOrder switch { "capacity" => q.OrderBy(r => r.Capacity).ThenBy(r => r.RoomId), "capacity_desc" => q.OrderByDescending(r => r.Capacity).ThenBy(r => r.RoomId), "name_desc" => q.OrderByDescending(r => r.RoomName).ThenBy(r => r.RoomId), _ => q.OrderBy(r => r.RoomName).ThenBy(r => r.RoomId) };
        return ServiceSupport.PageAsync(q.Select(Map), f, ct);
    }
    public Task<RoomViewModel?> GetAsync(int id, CancellationToken ct = default) => db.Rooms.AsNoTracking().Where(r => r.RoomId == id).Select(Map).FirstOrDefaultAsync(ct);
    public async Task<List<LookupItem>> GetLookupAsync(CancellationToken ct = default)
    {
        var items = await cache.GetOrCreateAsync(CacheKeys.Rooms, async entry => { entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5); return await db.Rooms.AsNoTracking().Where(r => !r.IsDisabled && r.Status == RoomStatus.Active).OrderBy(r => r.RoomName).Select(r => new LookupItem(r.RoomId, r.RoomName)).ToListAsync(ct); });
        return items!.ToList();
    }
    public async Task<ServiceResult> SaveAsync(RoomViewModel m, int actorUserId, CancellationToken ct = default)
    {
        if (!await ServiceSupport.IsAdminAsync(db, actorUserId, ct)) return ServiceResult.Failure("Bạn cần quyền quản trị viên.");
        var error = ServiceSupport.Validate(m); if (error != null) return ServiceResult.Failure(error);
        if (!Enum.IsDefined(m.Status)) return ServiceResult.Failure("Trạng thái phòng họp không hợp lệ.");
        var name = m.RoomName.Trim();
        if (name.Length == 0) return ServiceResult.Failure("Vui lòng nhập tên phòng họp.");
        if (await db.Rooms.AsNoTracking().AnyAsync(r => r.RoomName == name && r.RoomId != m.RoomId, ct)) return ServiceResult.Failure("Tên phòng họp đã tồn tại.");
        var room = m.RoomId == 0 ? new Room() : await db.Rooms.FindAsync([m.RoomId], ct);
        if (room == null) return ServiceResult.Failure("Không tìm thấy phòng họp.");
        if (m.RoomId == 0) db.Rooms.Add(room);
        room.RoomName = name; room.Location = m.Location?.Trim(); room.Equipment = m.Equipment?.Trim(); room.Capacity = m.Capacity; room.Status = m.Status; room.IsDisabled = m.IsDisabled;
        var result = await ServiceSupport.CommitAsync(db, actorUserId, m.RoomId == 0 ? "Create" : "Update", "Room", () => room.RoomId, ct);
        if (result.Succeeded) cache.Remove(CacheKeys.Rooms);
        return result;
    }
    public async Task<ServiceResult> DisableAsync(int id, int actorUserId, CancellationToken ct = default)
    {
        if (!await ServiceSupport.IsAdminAsync(db, actorUserId, ct)) return ServiceResult.Failure("Bạn cần quyền quản trị viên.");
        var room = await db.Rooms.FindAsync([id], ct); if (room == null) return ServiceResult.Failure("Không tìm thấy phòng họp.");
        room.IsDisabled = true;
        var result = await ServiceSupport.CommitAsync(db, actorUserId, "Disable", "Room", () => id, ct);
        if (result.Succeeded) cache.Remove(CacheKeys.Rooms);
        return result;
    }
}
