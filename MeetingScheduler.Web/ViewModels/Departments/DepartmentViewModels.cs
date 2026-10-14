using System.ComponentModel.DataAnnotations;
using MeetingScheduler.Web.ViewModels.Common;
namespace MeetingScheduler.Web.ViewModels.Departments;
public class DepartmentFilter : QueryFilter { public bool? IsDisabled { get; set; } = false; }
public class DepartmentViewModel
{
    public int DepartmentId { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập thông tin bắt buộc."), StringLength(100, ErrorMessage = "Độ dài tối đa là {1} ký tự.")] public string DepartmentName { get; set; } = "";
    public bool IsDisabled { get; set; }
}
