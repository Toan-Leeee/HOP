using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MeetingScheduler.Web.Models;
public class User
{
    [Key] public int UserId { get; set; }
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(100), Column(TypeName = "varchar(100)")] public string Email { get; set; } = "";
    [StringLength(20), Column(TypeName = "varchar(20)")] public string? Phone { get; set; }
    [Required, StringLength(255), Column(TypeName = "varchar(255)")] public string PasswordHash { get; set; } = "";
    public int RoleId { get; set; }
    public int DepartmentId { get; set; }
    public bool IsDisabled { get; set; }
    public Role Role { get; set; } = null!;
    public Department Department { get; set; } = null!;
    public ICollection<Meeting> OrganizedMeetings { get; set; } = new List<Meeting>();
    public ICollection<Meeting> CancelledMeetings { get; set; } = new List<Meeting>();
    public ICollection<MeetingParticipant> MeetingParticipants { get; set; } = new List<MeetingParticipant>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
