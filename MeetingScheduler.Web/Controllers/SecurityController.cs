using System.Security.Claims;
using MeetingScheduler.Web.Models;
using MeetingScheduler.Web.Services;
using MeetingScheduler.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MeetingScheduler.Web.Controllers;

[Authorize, Route("Account")]
public sealed class SecurityController(IUserActivityStateService activity, UserManager<User> userManager) : Controller
{
    [HttpGet("Security")]
    public async Task<IActionResult> Index()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id is null) return Challenge();
        var identityUser = await userManager.FindByIdAsync(id);
        if (identityUser is null || identityUser.IsDisabled) return Challenge();

            return View(new SecurityViewModel
        {
            FullName = identityUser.FullName,
            Email = identityUser.Email,
            DepartmentName = identityUser.Department.DepartmentName,
            RoleName = User.FindFirstValue(ClaimTypes.Role) ?? "Không xác định",
            Activity = activity.Read(HttpContext),
            CanManageMeetings = User.IsInRole("Organizer") || User.IsInRole("Admin"),
            CanManageSystem = User.IsInRole("Admin")
        });
    }

    [HttpPost("ResetActivity"), ValidateAntiForgeryToken]
    public IActionResult ResetUsage()
    {
        activity.ResetUsageCookie(HttpContext);
        TempData["Success"] = "Đã xóa hoạt động được ghi nhận trên thiết bị này.";
        return RedirectToAction(nameof(Index));
    }
}
