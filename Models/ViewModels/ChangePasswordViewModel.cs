using System.ComponentModel.DataAnnotations;

namespace WorkFlowBasic.Models.ViewModels;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Mevcut şifrenizi girmeniz gereklidir.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre zorunludur.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Yeni şifreler uyuşmuyor.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}