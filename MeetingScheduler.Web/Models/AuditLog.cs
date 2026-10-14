using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MeetingScheduler.Web.Models;
public class AuditLog
{
    [Key] public int AuditLogId { get; set; }
    public int UserId { get; set; }
    [Required, StringLength(100)] public string Action { get; set; } = "";
    [Required, StringLength(100)] public string EntityName { get; set; } = "";
    public int EntityId { get; set; }
    [Column(TypeName = "datetime")] public DateTime CreatedAt { get; set; }
    public string? Details { get; set; }
    public User User { get; set; } = null!;
}
