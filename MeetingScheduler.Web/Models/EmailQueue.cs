using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeetingScheduler.Web.Models;

/// <summary>Persistent email outbox record. Delivery is handled by the background email worker.</summary>
public class EmailQueue
{
    [Key]
    public int EmailQueueId { get; set; }

    [Required, StringLength(320)]
    public string RecipientEmail { get; set; } = "";

    [Required, StringLength(200)]
    public string Subject { get; set; } = "";

    [Required]
    public string Body { get; set; } = "";

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ScheduledAt { get; set; }

    [Required, StringLength(20)]
    public string Status { get; set; } = "Pending";

    public int AttemptCount { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? SentAt { get; set; }

    [StringLength(1000)]
    public string? LastError { get; set; }
}
