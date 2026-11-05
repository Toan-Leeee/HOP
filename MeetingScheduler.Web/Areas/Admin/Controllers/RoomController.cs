using MeetingScheduler.Web.Services.Interfaces;
using MeetingScheduler.Web.ViewModels.Rooms;
using Microsoft.AspNetCore.Mvc;

namespace MeetingScheduler.Web.Areas.Admin.Controllers;

public class RoomController(IRoomService rooms) : AdminController
{
    [HttpGet]
    public IActionResult Index(RoomFilter filter) => View("Ajax", filter);

    [HttpGet]
    public async Task<IActionResult> Traditional(RoomFilter filter, CancellationToken ct)
    {
        ViewBag.Filter = filter;
        return View("Index", await rooms.QueryAsync(filter, ct));
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ListJson(RoomFilter filter, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(new { message = "Bộ lọc không hợp lệ." });
        var result = await rooms.QueryAsync(filter, ct);
        var totalPages = (int)Math.Ceiling(result.TotalCount / (double)result.PageSize);
        if (result.Page > Math.Max(1, totalPages))
        {
            filter.Page = Math.Max(1, totalPages);
            result = await rooms.QueryAsync(filter, ct);
        }
        return Json(new {
            items = result.Items.Select(r => new { r.RoomId, r.RoomName, r.Location, r.Capacity,
                r.Equipment, status = r.Status.ToString(), r.IsDisabled }),
            result.TotalCount, result.Page, result.PageSize, totalPages
        });
    }

    [HttpPost]
    public async Task<IActionResult> SaveJson(RoomViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Vui lòng kiểm tra dữ liệu.", errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(x => x.Key, x => x.Value!.Errors.Select(e =>
                    string.IsNullOrEmpty(e.ErrorMessage) ? "Giá trị không hợp lệ." : e.ErrorMessage).ToArray()) });
        if (model.RoomId < 0) return BadRequest(new { message = "Mã phòng không hợp lệ." });
        if (model.RoomId == 0) model.IsDisabled = false;
        else
        {
            var existing = await rooms.GetAsync(model.RoomId, ct);
            if (existing is null) return NotFound(new { message = "Không tìm thấy phòng họp." });
            model.IsDisabled = existing.IsDisabled;
        }
        var result = await rooms.SaveAsync(model, CurrentUserId, ct);
        return result.Succeeded ? Json(new { message = "Đã lưu phòng họp.", id = result.EntityId })
            : BadRequest(new { message = result.Error });
    }

    [HttpPost]
    public async Task<IActionResult> DisableJson(int id, CancellationToken ct)
    {
        if (id <= 0) return BadRequest(new { message = "Mã phòng không hợp lệ." });
        var result = await rooms.DisableAsync(id, CurrentUserId, ct);
        return result.Succeeded ? Json(new { message = "Đã ngừng sử dụng phòng họp." })
            : BadRequest(new { message = result.Error });
    }

    [HttpPost]
    public async Task<IActionResult> RestoreJson(int id, CancellationToken ct)
    {
        var model = await rooms.GetAsync(id, ct);
        if (model is null) return NotFound(new { message = "Không tìm thấy phòng họp." });
        model.IsDisabled = false;
        var result = await rooms.SaveAsync(model, CurrentUserId, ct);
        return result.Succeeded ? Json(new { message = "Đã khôi phục phòng họp." })
            : BadRequest(new { message = result.Error });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var model = await rooms.GetAsync(id, ct);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet]
    public IActionResult Create() => View(new RoomViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(RoomViewModel model, CancellationToken ct)
    {
        model.RoomId = 0;
        model.IsDisabled = false;
        if (ModelState.IsValid)
        {
            var result = await rooms.SaveAsync(model, CurrentUserId, ct);
            if (result.Succeeded) return RedirectToAction(nameof(Index));
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể thêm phòng họp.");
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var model = await rooms.GetAsync(id, ct);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, RoomViewModel model, CancellationToken ct)
    {
        if (id <= 0 || id != model.RoomId) return BadRequest();
        if (ModelState.IsValid)
        {
            var result = await rooms.SaveAsync(model, CurrentUserId, ct);
            if (result.Succeeded) return RedirectToAction(nameof(Index));
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể cập nhật phòng họp.");
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var model = await rooms.GetAsync(id, ct);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        var result = await rooms.DisableAsync(id, CurrentUserId, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Đã ngừng sử dụng phòng họp." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public Task<IActionResult> Lock(int id, CancellationToken ct) => DeleteConfirmed(id, ct);

    [HttpPost]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var model = await rooms.GetAsync(id, ct);
        if (model is null) return NotFound();
        model.IsDisabled = false;
        var result = await rooms.SaveAsync(model, CurrentUserId, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Đã khôi phục phòng họp." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
