using MeetingScheduler.Web.Services.Interfaces;
using MeetingScheduler.Web.ViewModels.Users;
using Microsoft.AspNetCore.Mvc;

namespace MeetingScheduler.Web.Areas.Admin.Controllers;

public class UserController(IUserService users, IDepartmentService departments) : AdminController
{
    [HttpGet]
    public async Task<IActionResult> Index(UserFilter filter, CancellationToken ct)
    {
        ViewBag.Filter = filter;
        await PopulateLookupsAsync(ct);
        return View(await users.QueryAsync(filter, ct));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var model = await users.GetAsync(id, ct);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        await PopulateLookupsAsync(ct);
        return View(new UserCreateViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(UserCreateViewModel model, CancellationToken ct)
    {
        model.UserId = 0;
        model.IsDisabled = false;
        if (ModelState.IsValid)
        {
            var result = await users.CreateAsync(model, CurrentUserId, ct);
            if (result.Succeeded) return RedirectToAction(nameof(Index));
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể thêm tài khoản.");
        }
        await PopulateLookupsAsync(ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var model = await users.GetAsync(id, ct);
        if (model is null) return NotFound();
        await PopulateLookupsAsync(ct);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, UserEditViewModel model, CancellationToken ct)
    {
        if (id <= 0 || id != model.UserId) return BadRequest();
        if (ModelState.IsValid)
        {
            var result = await users.UpdateAsync(model, CurrentUserId, ct);
            if (result.Succeeded) return RedirectToAction(nameof(Index));
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể cập nhật tài khoản.");
        }
        await PopulateLookupsAsync(ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var model = await users.GetAsync(id, ct);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        var result = await users.DisableAsync(id, CurrentUserId, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Đã khóa tài khoản." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public Task<IActionResult> Lock(int id, CancellationToken ct) => DeleteConfirmed(id, ct);

    [HttpPost]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var model = await users.GetAsync(id, ct);
        if (model is null) return NotFound();
        model.IsDisabled = false;
        var result = await users.UpdateAsync(model, CurrentUserId, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Đã khôi phục tài khoản." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateLookupsAsync(CancellationToken ct)
    {
        ViewBag.Departments = await departments.GetLookupAsync(ct);
        ViewBag.Roles = await users.GetRolesAsync(ct);
    }
}
