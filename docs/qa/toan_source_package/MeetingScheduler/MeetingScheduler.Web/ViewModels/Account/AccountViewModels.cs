using System.ComponentModel.DataAnnotations;
namespace MeetingScheduler.Web.ViewModels.Account;
public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập thông tin bắt buộc."), EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")] public string Email { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập thông tin bắt buộc."), DataType(DataType.Password)] public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}
public record AuthenticatedUser(int UserId, string FullName, string Email, string RoleName);
public class ProfileViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập thông tin bắt buộc."), StringLength(100, ErrorMessage = "Độ dài tối đa là {1} ký tự.")] public string FullName { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập thông tin bắt buộc."), EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ."), StringLength(100, ErrorMessage = "Độ dài tối đa là {1} ký tự.")] public string Email { get; set; } = "";
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ."), StringLength(20, ErrorMessage = "Độ dài tối đa là {1} ký tự.")] public string? Phone { get; set; }
}
public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập thông tin bắt buộc."), DataType(DataType.Password)] public string CurrentPassword { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập thông tin bắt buộc."), MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự."), StringLength(100, ErrorMessage = "Độ dài tối đa là {1} ký tự."), DataType(DataType.Password)] public string NewPassword { get; set; } = "";
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp."), DataType(DataType.Password)] public string ConfirmPassword { get; set; } = "";
}
