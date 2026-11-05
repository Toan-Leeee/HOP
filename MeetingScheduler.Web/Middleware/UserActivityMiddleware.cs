using MeetingScheduler.Web.Services;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace MeetingScheduler.Web.Middleware;

public sealed class UserActivityMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IUserActivityStateService state)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && HttpMethods.IsGet(context.Request.Method)
            && context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>() is { } action)
        {
            state.RecordRequest(context, FeatureName(action.ControllerName, action.ActionName));
        }
        await next(context);
    }

    private static string FeatureName(string controller, string action) => (controller, action) switch
    {
        ("Meeting", "Index") => "Xem lịch họp",
        ("Meeting", "Details") => "Xem chi tiết lịch họp",
        ("Meeting", "Create") => "Tạo lịch họp",
        ("Meeting", "Edit") => "Cập nhật lịch họp",
        ("Meeting", "Cancel") => "Hủy lịch họp",
        ("Meeting", "RsvpTracking") => "Theo dõi RSVP",
        ("Profile", _) => "Quản lý hồ sơ",
        ("Security", _) => "Thực hành Security",
        _ => $"{controller}/{action}"
    };
}
