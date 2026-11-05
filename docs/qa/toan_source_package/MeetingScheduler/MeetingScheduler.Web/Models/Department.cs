using System.ComponentModel.DataAnnotations;
namespace MeetingScheduler.Web.Models;
public class Department
{
    [Key] public int DepartmentId { get; set; }
    [Required, StringLength(100)] public string DepartmentName { get; set; } = "";
    public bool IsDisabled { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}
