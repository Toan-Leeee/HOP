using MeetingScheduler.Web.Services.Interfaces;
using MeetingScheduler.Web.ViewModels.Rooms;
using Microsoft.AspNetCore.Mvc;

namespace MeetingScheduler.Web.Areas.Admin.Controllers;

public class RoomController(IRoomService rooms) : AdminController
{
    [HttpGet]
    public async Task<IActionResult> Index(RoomFilter filter, CancellationToken ct)
    {
        ViewBag.Filter = filter;
        return View(await rooms.QueryAsync(filter, ct));
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
