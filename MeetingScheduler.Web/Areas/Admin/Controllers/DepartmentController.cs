using MeetingScheduler.Web.Services.Interfaces;
using MeetingScheduler.Web.ViewModels.Departments;
using Microsoft.AspNetCore.Mvc;

namespace MeetingScheduler.Web.Areas.Admin.Controllers;

public class DepartmentController(IDepartmentService departments) : AdminController
{
    [HttpGet]
    public async Task<IActionResult> Index(DepartmentFilter filter, CancellationToken ct)
    {
        ViewBag.Filter = filter;
        return View(await departments.QueryAsync(filter, ct));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var model = await departments.GetAsync(id, ct);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet]
    public IActionResult Create() => View(new DepartmentViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(DepartmentViewModel model, CancellationToken ct)
    {
        model.DepartmentId = 0;
        model.IsDisabled = false;
        if (ModelState.IsValid)
        {
            var result = await departments.SaveAsync(model, CurrentUserId, ct);
            if (result.Succeeded) return RedirectToAction(nameof(Index));
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể thêm phòng ban.");
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var model = await departments.GetAsync(id, ct);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, DepartmentViewModel model, CancellationToken ct)
    {
        if (id <= 0 || id != model.DepartmentId) return BadRequest();
        if (ModelState.IsValid)
        {
            var result = await departments.SaveAsync(model, CurrentUserId, ct);
            if (result.Succeeded) return RedirectToAction(nameof(Index));
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể cập nhật phòng ban.");
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var model = await departments.GetAsync(id, ct);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        var result = await departments.DisableAsync(id, CurrentUserId, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Đã ngừng sử dụng phòng ban." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public Task<IActionResult> Lock(int id, CancellationToken ct) => DeleteConfirmed(id, ct);

    [HttpPost]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var model = await departments.GetAsync(id, ct);
        if (model is null) return NotFound();
        model.IsDisabled = false;
        var result = await departments.SaveAsync(model, CurrentUserId, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Đã khôi phục phòng ban." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
