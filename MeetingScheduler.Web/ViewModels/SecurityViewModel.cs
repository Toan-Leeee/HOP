using MeetingScheduler.Web.Services;

namespace MeetingScheduler.Web.ViewModels;

public sealed class SecurityViewModel
{
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public required string DepartmentName { get; init; }
    public required string RoleName { get; init; }
    public required UserActivitySnapshot Activity { get; init; }
    public bool CanManageMeetings { get; init; }
    public bool CanManageSystem { get; init; }
}
