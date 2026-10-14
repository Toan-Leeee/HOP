using System.ComponentModel.DataAnnotations;
using MeetingScheduler.Web.ViewModels.Common;
namespace MeetingScheduler.Web.ViewModels.Users;
public class UserFilter : QueryFilter
{
    public int? DepartmentId { get; set; }
    public int? RoleId { get; set; }
    public bool? IsDisabled { get; set; } = false;
}
public class UserEditViewModel
{
    public int UserId { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập thông tin bắt buộc."), StringLength(100, ErrorMessage = "Độ dài tối đa là {1} ký tự.")] public string FullName { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập thông tin bắt buộc."), EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ."), StringLength(100, ErrorMessage = "Độ dài tối đa là {1} ký tự.")] public string Email { get; set; } = "";
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ."), StringLength(20, ErrorMessage = "Độ dài tối đa là {1} ký tự.")] public string? Phone { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Giá trị phải lớn hơn 0.")] public int DepartmentId { get; set; }
    [Range(1, 3, ErrorMessage = "Vai trò không hợp lệ.")] public int RoleId { get; set; } = 3;
    public bool IsDisabled { get; set; }
    public string? DepartmentName { get; set; }
    public string? RoleName { get; set; }
}
public class UserCreateViewModel : UserEditViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập thông tin bắt buộc."), MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự."), StringLength(100, ErrorMessage = "Độ dài tối đa là {1} ký tự."), DataType(DataType.Password)] public string Password { get; set; } = "";
}
