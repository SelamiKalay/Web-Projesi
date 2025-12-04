using System.ComponentModel.DataAnnotations;

namespace WorkFlowBasic.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email adresi zorunludur.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}