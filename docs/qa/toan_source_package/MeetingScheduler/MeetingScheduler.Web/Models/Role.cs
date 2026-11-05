using System.ComponentModel.DataAnnotations;
namespace MeetingScheduler.Web.Models;
public class Role
{
    [Key] public int RoleId { get; set; }
    [Required, StringLength(50)] public string RoleName { get; set; } = "";
    public ICollection<User> Users { get; set; } = new List<User>();
}
