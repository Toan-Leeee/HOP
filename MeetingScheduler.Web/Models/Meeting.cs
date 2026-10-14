using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MeetingScheduler.Web.Models.Enums;
namespace MeetingScheduler.Web.Models;
public class Meeting : IValidatableObject
{
    [Key] public int MeetingId { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = "";
    public string? Description { get; set; }
    [Column(TypeName = "datetime")] public DateTime StartTime { get; set; }
    [Column(TypeName = "datetime")] public DateTime EndTime { get; set; }
    public int RoomId { get; set; }
    public int CreatedByUserId { get; set; }
    public MeetingStatus Status { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
    [Column(TypeName = "datetime")] public DateTime? CancelledAt { get; set; }
    public int? CancelledByUserId { get; set; }
    [StringLength(500)] public string? CancelReason { get; set; }
    [Column(TypeName = "datetime")] public DateTime CreatedAt { get; set; }
    [Column(TypeName = "datetime")] public DateTime? UpdatedAt { get; set; }
    public Room Room { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public User? CancelledByUser { get; set; }
    public ICollection<MeetingParticipant> Participants { get; set; } = new List<MeetingParticipant>();
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (EndTime <= StartTime) yield return new ValidationResult("EndTime must be after StartTime.", [nameof(EndTime)]);
    }
}
