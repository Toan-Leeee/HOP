using System.ComponentModel.DataAnnotations;
using MeetingScheduler.Web.Models.Enums;
namespace MeetingScheduler.Web.Models;
public class Room
{
    [Key] public int RoomId { get; set; }
    [Required, StringLength(100)] public string RoomName { get; set; } = "";
    [StringLength(200)] public string? Location { get; set; }
    [Range(1, int.MaxValue)] public int Capacity { get; set; }
    [StringLength(500)] public string? Equipment { get; set; }
    public RoomStatus Status { get; set; }
    public bool IsDisabled { get; set; }
    public ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();
}
