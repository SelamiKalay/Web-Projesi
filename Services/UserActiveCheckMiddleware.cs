using Microsoft.AspNetCore.Identity;
using WorkFlowBasic.Models;

namespace WorkFlowBasic.Services;

public class UserActiveCheckMiddleware
{
    private readonly RequestDelegate _next;

    public UserActiveCheckMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        // 1. Kullanıcı giriş yapmış mı?
        if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
        {
            // 2. Veritabanından güncel durumunu çek
            var user = await userManager.GetUserAsync(context.User);

            if (user != null)
            {
                // 3. KONTROL: Hesabı kilitli mi (Lockout) VEYA Pasife alınmış mı (Deactivated)?
                bool isLocked = await userManager.IsLockedOutAsync(user);
                bool isDeactivated = user.DeactivatedDate != null;

                if (isLocked || isDeactivated)
                {
                    // 4. EĞER YASAKLIYSA:
                    // Oturumu kapat (Çerezi sil)
                    await signInManager.SignOutAsync();

                    // Giriş sayfasına yönlendir
                    context.Response.Redirect("/Account/Login?error=account-locked");
                    return; // İşlemi kes, sayfayı gösterme
                }
            }
        }

        // Sorun yoksa sıradaki işlemi yap (Sayfayı göster)
        await _next(context);
    }
}