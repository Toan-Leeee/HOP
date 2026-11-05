using MeetingScheduler.Web.Data;
using MeetingScheduler.Web.Infrastructure;
using MeetingScheduler.Web.Models;
using MeetingScheduler.Web.Services.Interfaces;
using MeetingScheduler.Web.ViewModels.Common;
using MeetingScheduler.Web.ViewModels.Departments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
namespace MeetingScheduler.Web.Services;
public class DepartmentService(ApplicationDbContext db, IMemoryCache cache) : IDepartmentService
{
    public Task<PagedResult<DepartmentViewModel>> QueryAsync(DepartmentFilter f, CancellationToken ct = default)
    {
        var q = db.Departments.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(f.SearchString)) q = q.Where(d => d.DepartmentName.Contains(f.SearchString.Trim()));
        if (f.IsDisabled.HasValue) q = q.Where(d => d.IsDisabled == f.IsDisabled);
        q = f.SortOrder == "name_desc" ? q.OrderByDescending(d => d.DepartmentName).ThenBy(d => d.DepartmentId) : q.OrderBy(d => d.DepartmentName).ThenBy(d => d.DepartmentId);
        return ServiceSupport.PageAsync(q.Select(d => new DepartmentViewModel { DepartmentId = d.DepartmentId, DepartmentName = d.DepartmentName, IsDisabled = d.IsDisabled }), f, ct);
    }
    public Task<DepartmentViewModel?> GetAsync(int id, CancellationToken ct = default) => db.Departments.AsNoTracking().Where(d => d.DepartmentId == id).Select(d => new DepartmentViewModel { DepartmentId = d.DepartmentId, DepartmentName = d.DepartmentName, IsDisabled = d.IsDisabled }).FirstOrDefaultAsync(ct);
    public async Task<List<LookupItem>> GetLookupAsync(CancellationToken ct = default)
    {
        var items = await cache.GetOrCreateAsync(CacheKeys.Departments, async entry => { entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5); return await db.Departments.AsNoTracking().Where(d => !d.IsDisabled).OrderBy(d => d.DepartmentName).Select(d => new LookupItem(d.DepartmentId, d.DepartmentName)).ToListAsync(ct); });
        return items!.ToList();
    }
    public async Task<ServiceResult> SaveAsync(DepartmentViewModel m, int actorUserId, CancellationToken ct = default)
    {
        if (!await ServiceSupport.IsAdminAsync(db, actorUserId, ct)) return ServiceResult.Failure("Bạn cần quyền quản trị viên.");
        var error = ServiceSupport.Validate(m); if (error != null) return ServiceResult.Failure(error);
        var name = m.DepartmentName.Trim(); if (name.Length == 0) return ServiceResult.Failure("Vui lòng nhập tên phòng ban.");
        if (m.IsDisabled && await db.Users.AsNoTracking().AnyAsync(u => u.UserId == actorUserId && u.DepartmentId == m.DepartmentId, ct)) return ServiceResult.Failure("Bạn không thể khóa phòng ban của chính tài khoản quản trị đang sử dụng.");
        if (m.IsDisabled && await db.Users.AsNoTracking().AnyAsync(u => u.DepartmentId == m.DepartmentId && !u.IsDisabled, ct)) return ServiceResult.Failure("Hãy chuyển hoặc khóa các tài khoản đang hoạt động trước khi khóa phòng ban.");
        if (await db.Departments.AsNoTracking().AnyAsync(d => d.DepartmentName == name && d.DepartmentId != m.DepartmentId, ct)) return ServiceResult.Failure("Tên phòng ban đã tồn tại.");
        var department = m.DepartmentId == 0 ? new Department() : await db.Departments.FindAsync([m.DepartmentId], ct);
        if (department == null) return ServiceResult.Failure("Không tìm thấy phòng ban.");
        if (m.DepartmentId == 0) db.Departments.Add(department);
        department.DepartmentName = name; department.IsDisabled = m.IsDisabled;
        var result = await ServiceSupport.CommitAsync(db, actorUserId, m.DepartmentId == 0 ? "Create" : "Update", "Department", () => department.DepartmentId, ct);
        if (result.Succeeded) cache.Remove(CacheKeys.Departments);
        return result;
    }
    public async Task<ServiceResult> DisableAsync(int id, int actorUserId, CancellationToken ct = default)
    {
        if (!await ServiceSupport.IsAdminAsync(db, actorUserId, ct)) return ServiceResult.Failure("Bạn cần quyền quản trị viên.");
        var department = await db.Departments.FindAsync([id], ct); if (department == null) return ServiceResult.Failure("Không tìm thấy phòng ban.");
        if (await db.Users.AsNoTracking().AnyAsync(u => u.UserId == actorUserId && u.DepartmentId == id, ct)) return ServiceResult.Failure("Bạn không thể khóa phòng ban của chính tài khoản quản trị đang sử dụng.");
        if (await db.Users.AsNoTracking().AnyAsync(u => u.DepartmentId == id && !u.IsDisabled, ct)) return ServiceResult.Failure("Hãy chuyển hoặc khóa các tài khoản đang hoạt động trước khi khóa phòng ban.");
        department.IsDisabled = true;
        var result = await ServiceSupport.CommitAsync(db, actorUserId, "Disable", "Department", () => id, ct);
        if (result.Succeeded) cache.Remove(CacheKeys.Departments);
        return result;
    }
}
