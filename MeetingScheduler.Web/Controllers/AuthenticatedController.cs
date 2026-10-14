using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MeetingScheduler.Web.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
public abstract class AuthenticatedController : Controller
{
    protected int CurrentUserId { get; private set; }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId <= 0)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.Result = Challenge(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        CurrentUserId = userId;
        await next();
    }
}
