using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MeetingScheduler.Web.Models.Enums;
namespace MeetingScheduler.Web.Models;
public class MeetingParticipant
{
    [Required] public int MeetingId { get; set; }
    [Required] public int UserId { get; set; }
    public RsvpStatus Status { get; set; }
    [Column(TypeName = "datetime")] public DateTime? RespondedAt { get; set; }
    public Meeting Meeting { get; set; } = null!;
    public User User { get; set; } = null!;
}
