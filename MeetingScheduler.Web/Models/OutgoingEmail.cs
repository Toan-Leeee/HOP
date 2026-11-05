using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeetingScheduler.Web.Models;

public class OutgoingEmail
{
    public long OutgoingEmailId { get; set; }
    [Required, StringLength(320)] public string To { get; set; } = "";
    [Required, StringLength(200)] public string Subject { get; set; } = "";
    [Required] public string Body { get; set; } = "";
    public int Attempts { get; set; }
    public int? MeetingId { get; set; }
    [StringLength(40)] public string Kind { get; set; } = "Notification";
    [Column(TypeName = "datetime")] public DateTime AvailableAt { get; set; }
    [Column(TypeName = "datetime")] public DateTime? SentAt { get; set; }
    [StringLength(1000)] public string? LastError { get; set; }
}
