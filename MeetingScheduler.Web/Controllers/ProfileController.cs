using System.Security.Claims;
using MeetingScheduler.Web.Services.Interfaces;
using MeetingScheduler.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace MeetingScheduler.Web.Controllers;

public class ProfileController(IAccountService accounts) : AuthenticatedController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var model = await accounts.GetProfileAsync(CurrentUserId, ct);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Index(ProfileViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await accounts.UpdateProfileAsync(CurrentUserId, model, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể cập nhật hồ sơ.");
            return View(model);
        }

        var authentication = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var identity = new ClaimsIdentity(User.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
        foreach (var claim in identity.FindAll(ClaimTypes.Name).ToArray()) identity.RemoveClaim(claim);
        foreach (var claim in identity.FindAll(ClaimTypes.Email).ToArray()) identity.RemoveClaim(claim);
        identity.AddClaim(new Claim(ClaimTypes.Name, model.FullName));
        identity.AddClaim(new Claim(ClaimTypes.Email, model.Email));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), authentication.Properties);
        TempData["Success"] = "Đã cập nhật hồ sơ.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await accounts.ChangePasswordAsync(CurrentUserId, model, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể đổi mật khẩu.");
            return View(model);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Success"] = "Đã đổi mật khẩu. Vui lòng đăng nhập lại.";
        return RedirectToAction("Login", "Account");
    }
}
