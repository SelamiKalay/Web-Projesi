namespace WorkFlowBasic.Services;

// Yüklenen dosyalar wwwroot altında sunulduğu için yalnızca güvenli uzantılara izin verilir.
// .html, .svg, .js gibi dosyalar site adresinden çalıştırılıp XSS'e yol açabilir.
public static class FileUploadHelper
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt",
        ".png", ".jpg", ".jpeg", ".gif", ".zip", ".rar"
    };

    public const string ErrorMessage = "Bu dosya türü desteklenmiyor. İzin verilenler: PDF, Office belgeleri, TXT, resim (PNG/JPG/GIF), ZIP/RAR.";

    public static bool IsAllowed(string? extension) =>
        !string.IsNullOrEmpty(extension) && AllowedExtensions.Contains(extension);
}
