using System.ComponentModel.DataAnnotations;
using MeetingScheduler.Web.Models.Enums;
using MeetingScheduler.Web.ViewModels.Common;
namespace MeetingScheduler.Web.ViewModels.Rooms;
public class RoomFilter : QueryFilter
{
    public RoomStatus? Status { get; set; }
    public int? MinimumCapacity { get; set; }
    public bool? IsDisabled { get; set; } = false;
}
public class RoomViewModel
{
    public int RoomId { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập thông tin bắt buộc."), StringLength(100, ErrorMessage = "Độ dài tối đa là {1} ký tự.")] public string RoomName { get; set; } = "";
    [StringLength(200, ErrorMessage = "Độ dài tối đa là {1} ký tự.")] public string? Location { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Giá trị phải lớn hơn 0.")] public int Capacity { get; set; } = 1;
    [StringLength(500, ErrorMessage = "Độ dài tối đa là {1} ký tự.")] public string? Equipment { get; set; }
    public RoomStatus Status { get; set; }
    public bool IsDisabled { get; set; }
}
