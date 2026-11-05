using System.ComponentModel.DataAnnotations;
using MeetingScheduler.Web.Models.Enums;
using MeetingScheduler.Web.ViewModels.Common;
namespace MeetingScheduler.Web.ViewModels.Meetings;
public class MeetingFilter : QueryFilter
{
    public int? RoomId { get; set; }
    public int? CreatedByUserId { get; set; }
    public MeetingStatus? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
public class MeetingEditViewModel : IValidatableObject
{
    public int MeetingId { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập thông tin bắt buộc."), StringLength(200, ErrorMessage = "Độ dài tối đa là {1} ký tự.")] public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime StartTime { get; set; } = DateTime.Now.AddHours(1);
    public DateTime EndTime { get; set; } = DateTime.Now.AddHours(2);
    [Range(1, int.MaxValue, ErrorMessage = "Giá trị phải lớn hơn 0.")] public int RoomId { get; set; }
    public List<int> ParticipantIds { get; set; } = [];
    public byte[] RowVersion { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (EndTime <= StartTime) yield return new("Thời gian kết thúc phải sau thời gian bắt đầu.", [nameof(EndTime)]);
    }
}
public class MeetingDetailsViewModel : MeetingEditViewModel
{
    public string RoomName { get; set; } = "";
    public int CreatedByUserId { get; set; }
    public string OrganizerName { get; set; } = "";
    public MeetingStatus Status { get; set; }
    public string? CancelReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public int ParticipantCount { get; set; }
}
public class CancelMeetingViewModel
{
    public int MeetingId { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập thông tin bắt buộc."), StringLength(500, ErrorMessage = "Độ dài tối đa là {1} ký tự.")] public string CancelReason { get; set; } = "";
    public byte[] RowVersion { get; set; } = [];
}
public class RsvpFilter : QueryFilter
{
    public int MeetingId { get; set; }
    public RsvpStatus? Status { get; set; }
}
public class RsvpTrackingViewModel
{
    public int MeetingId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public RsvpStatus Status { get; set; }
    public DateTime? RespondedAt { get; set; }
}
