using MeetingScheduler.Web.ViewModels.Common;
using MeetingScheduler.Web.ViewModels.Rooms;
using MeetingScheduler.Web.ViewModels.Departments;
using MeetingScheduler.Web.ViewModels.Users;
using MeetingScheduler.Web.ViewModels.Account;
using MeetingScheduler.Web.ViewModels.Meetings;
using MeetingScheduler.Web.ViewModels.Reports;
using MeetingScheduler.Web.Models.Enums;
namespace MeetingScheduler.Web.Services.Interfaces;
public interface IRoomService
{
    Task<PagedResult<RoomViewModel>> QueryAsync(RoomFilter filter, CancellationToken ct = default);
    Task<RoomViewModel?> GetAsync(int id, CancellationToken ct = default);
    Task<List<LookupItem>> GetLookupAsync(CancellationToken ct = default);
    Task<ServiceResult> SaveAsync(RoomViewModel model, int actorUserId, CancellationToken ct = default);
    Task<ServiceResult> DisableAsync(int id, int actorUserId, CancellationToken ct = default);
}
public interface IDepartmentService
{
    Task<PagedResult<DepartmentViewModel>> QueryAsync(DepartmentFilter filter, CancellationToken ct = default);
    Task<DepartmentViewModel?> GetAsync(int id, CancellationToken ct = default);
    Task<List<LookupItem>> GetLookupAsync(CancellationToken ct = default);
    Task<ServiceResult> SaveAsync(DepartmentViewModel model, int actorUserId, CancellationToken ct = default);
    Task<ServiceResult> DisableAsync(int id, int actorUserId, CancellationToken ct = default);
}
public interface IUserService
{
    Task<PagedResult<UserEditViewModel>> QueryAsync(UserFilter filter, CancellationToken ct = default);
    Task<UserEditViewModel?> GetAsync(int id, CancellationToken ct = default);
    Task<List<LookupItem>> GetLookupAsync(CancellationToken ct = default);
    Task<List<LookupItem>> GetRolesAsync(CancellationToken ct = default);
    Task<ServiceResult> CreateAsync(UserCreateViewModel model, int actorUserId, CancellationToken ct = default);
    Task<ServiceResult> UpdateAsync(UserEditViewModel model, int actorUserId, CancellationToken ct = default);
    Task<ServiceResult> DisableAsync(int id, int actorUserId, CancellationToken ct = default);
}
public interface IAccountService
{
    Task<bool> ValidateSessionAsync(int userId, string roleName, CancellationToken ct = default);
    Task<AuthenticatedUser?> LoginAsync(LoginViewModel model, CancellationToken ct = default);
    Task<ProfileViewModel?> GetProfileAsync(int userId, CancellationToken ct = default);
    Task<ServiceResult> UpdateProfileAsync(int userId, ProfileViewModel model, CancellationToken ct = default);
    Task<ServiceResult> ChangePasswordAsync(int userId, ChangePasswordViewModel model, CancellationToken ct = default);
}
public interface IMeetingService
{
    Task<PagedResult<MeetingDetailsViewModel>> QueryAsync(MeetingFilter filter, int actorUserId, CancellationToken ct = default);
    Task<MeetingDetailsViewModel?> GetAsync(int id, int actorUserId, CancellationToken ct = default);
    Task<ServiceResult> SaveAsync(MeetingEditViewModel model, int actorUserId, CancellationToken ct = default);
    Task<ServiceResult> CancelAsync(CancelMeetingViewModel model, int actorUserId, CancellationToken ct = default);
}
public interface IRsvpService
{
    Task<PagedResult<RsvpTrackingViewModel>?> QueryAsync(RsvpFilter filter, int actorUserId, CancellationToken ct = default);
    Task<ServiceResult> RespondAsync(int meetingId, int actorUserId, RsvpStatus status, CancellationToken ct = default);
}
public interface IRoomAvailabilityService
{
    Task<List<RoomViewModel>> GetAvailableAsync(DateTime start, DateTime end, int minimumCapacity = 1, int? excludeMeetingId = null, CancellationToken ct = default);
}
public interface IReportService { Task<ReportResult> GetAsync(ReportFilter filter, CancellationToken ct = default); }
public interface IAuditService { Task LogAsync(int userId, string action, string entityName, int entityId, string? details = null, CancellationToken ct = default); }
