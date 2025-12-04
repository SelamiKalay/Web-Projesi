using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // Dosya yükleme (IFormFile) için şart
using System.Xml.Linq;

namespace WorkFlowBasic.Models.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Ad alanı zorunludur.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad alanı zorunludur.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email alanı zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
    public string Email { get; set; } = string.Empty;

    // YENİ: Telefon
    [Required(ErrorMessage = "Telefon numarası zorunludur.")]
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
    [Display(Name = "Telefon Numarası")]
    public string PhoneNumber { get; set; } = string.Empty;

    // YENİ: CV Dosyası (Veritabanına gitmez, sunucuya kaydedilir)
    [Display(Name = "CV Yükle (PDF/Word)")]
    public IFormFile? CVFile { get; set; }

    [Required(ErrorMessage = "Şifre alanı zorunludur.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor.")]
    [Display(Name = "Şifre Tekrar")]
    public string ConfirmNewPassword { get; set; } = string.Empty;

    public string Department { get; set; } = "Genel";
}