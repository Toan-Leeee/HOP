using System.Security.Claims;
using MeetingScheduler.Web.Services;
using MeetingScheduler.Web.Services.Interfaces;
using MeetingScheduler.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeetingScheduler.Web.Controllers;

[AutoValidateAntiforgeryToken]
public class AccountController(IAccountService accounts, IUserActivityStateService activity) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel
    {
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null
    });

    [AllowAnonymous, HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        var resetUrl = Url.Action(nameof(ResetPassword), "Account", new { email = model.Email }, Request.Scheme)!;
        await accounts.RequestPasswordResetAsync(model.Email, resetUrl, ct);
        TempData["ResetNotice"] = "Nếu email thuộc tài khoản đang hoạt động, hướng dẫn đặt lại mật khẩu sẽ được gửi.";
        return RedirectToAction(nameof(ForgotPassword));
    }

    [AllowAnonymous, HttpGet]
    public IActionResult ResetPassword(string email, string token) => View(new ResetPasswordViewModel { Email = email, Token = token });

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        if (!await accounts.ResetPasswordAsync(model, ct))
        {
            ModelState.AddModelError(string.Empty, "Liên kết không hợp lệ hoặc đã hết hạn. Hãy yêu cầu liên kết mới.");
            return View(model);
        }
        TempData["Success"] = "Mật khẩu đã được đặt lại. Bạn có thể đăng nhập.";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        var account = await accounts.LoginAsync(model, ct);
        if (account is null)
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng, hoặc tài khoản đã bị khóa.");
            return View(model);
        }

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, account.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, account.FullName),
            new Claim(ClaimTypes.Email, account.Email),
            new Claim(ClaimTypes.Role, account.RoleName)
        }, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = model.RememberMe });
        activity.StartSession(HttpContext, account.UserId, account.RoleName);
        return Url.IsLocalUrl(model.ReturnUrl)
            ? LocalRedirect(model.ReturnUrl!)
            : RedirectToAction("Index", "Meeting");
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> Logout()
    {
        activity.ClearSession(HttpContext);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous, HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
