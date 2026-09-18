using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Include ve ToListAsync için gerekli
using WorkFlowBasic.Data;            // DbContext için gerekli
using WorkFlowBasic.Models;
using WorkFlowBasic.Models.ViewModels;
using WorkFlowBasic.Services;        // Notification Service için gerekli

namespace WorkFlowBasic.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context; // Veritabanı
    private readonly NotificationService _notificationService; // Bildirim Servisi

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        NotificationService notificationService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
        _notificationService = notificationService;
    }

    // 1. GİRİŞ SAYFASI
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity!.IsAuthenticated) return RedirectToAction("Index", "Home");
        return View();
    }

    // 2. GİRİŞ İŞLEMİ
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null && !user.EmailConfirmed)
            {
                ModelState.AddModelError("", "Hesabınız henüz yönetici tarafından onaylanmadı.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, true);

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Hesabınız kilitlenmiştir (İşten Çıkarıldı).");
                return View(model);
            }

            ModelState.AddModelError("", "Geçersiz giriş denemesi.");
        }
        return View(model);
    }

    // 3. KAYIT SAYFASI
    [HttpGet]
    public IActionResult Register() => View();

    // 4. KAYIT İŞLEMİ
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var history = await _context.ApplicantHistories
                .Where(x => x.Email == model.Email)
                .OrderByDescending(x => x.ActionDate)
                .ToListAsync();

            if (history.Any())
            {
                // Bugün kaç kere reddedildi?
                var todayRejections = history.Count(x => x.ActionDate.Date == DateTime.UtcNow.AddHours(3).Date);

                // En son ne zaman işlem görmüş?
                var lastActionDate = history.First().ActionDate;

                // KURAL: Eğer bugün 3 kere reddedildiyse VE son işlemden 7 gün geçmediyse
                if (todayRejections >= 3 && (DateTime.UtcNow.AddHours(3) - lastActionDate).TotalDays < 7)
                {
                    var remainingDays = 7 - (int)(DateTime.UtcNow.AddHours(3) - lastActionDate).TotalDays;
                    ModelState.AddModelError("", $"Çok fazla başarısız başvuru yaptınız. Güvenlik nedeniyle {remainingDays} gün boyunca yeni başvuru yapamazsınız.");
                    return View(model);
                }
            }
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Department = model.Department,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = false
            };

            if (model.CVFile != null && model.CVFile.Length > 0)
            {
                var ext = Path.GetExtension(model.CVFile.FileName);
                if (!FileUploadHelper.IsAllowed(ext))
                {
                    ModelState.AddModelError("", FileUploadHelper.ErrorMessage);
                    return View(model);
                }
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "cvs");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                var fileName = $"CV_{Guid.NewGuid()}{ext}";
                using (var stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                {
                    await model.CVFile.CopyToAsync(stream);
                }
                user.CVFilePath = $"/cvs/{fileName}";
            }

            var manager = await _userManager.FindByEmailAsync("mehmet@sirket.com");
            if (manager != null) user.ManagerId = manager.Id;

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Personel");
                TempData["Success"] = "Başvuru alındı.";

                // Müdüre Bildirim Gönder
                if (manager != null)
                {
                    await _notificationService.SendNotificationAsync(manager.Id, "Yeni Başvuru", $"{user.FullName} kayıt oldu.", "/Account/PendingUsers");
                }

                return RedirectToAction("Login");
            }
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        }
        return View(model);
    }

    // 5. ÇIKIŞ
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", "Account");
    }

    // 6. ONAY BEKLEYENLER
    [Authorize(Roles = "Manager,Admin")]
    public IActionResult PendingUsers() => View(_userManager.Users.Where(u => !u.EmailConfirmed).ToList());

    // 7. ONAYLA
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> ApproveUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
            TempData["Success"] = "Kullanıcı onaylandı.";

            // Kullanıcıya Mail Gidebilir (Opsiyonel)
        }
        return RedirectToAction(nameof(PendingUsers));
    }

    // 7B. REDDET (KARA LİSTE KAYDI İLE)
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> RejectUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        var currentUser = await _userManager.GetUserAsync(User);

        if (user != null)
        {
            // --- GEÇMİŞE KAYDET ---
            var history = new ApplicantHistory
            {
                Email = user.Email!,
                FullName = user.FullName,
                Result = "Reddedildi",
                ManagerName = currentUser?.FullName ?? "Sistem",
                ManagerNote = "Hızlı reddedildi.",
                ActionDate = DateTime.UtcNow.AddHours(3)
            };
            _context.ApplicantHistories.Add(history);
            await _context.SaveChangesAsync();
            // ----------------------

            await _userManager.DeleteAsync(user);
            TempData["Error"] = "Başvuru reddedildi ve not edildi.";
        }
        return RedirectToAction(nameof(PendingUsers));
    }

    // 8. İNCELE (GET)
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> ReviewUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        // Geçmiş Başvuruları Getir
        var previousApps = await _context.ApplicantHistories
            .Where(h => h.Email == user.Email)
            .OrderByDescending(h => h.ActionDate)
            .ToListAsync();

        ViewBag.History = previousApps;

        return View(user);
    }

    // 8B. İNCELE (POST)
    [Authorize(Roles = "Manager,Admin")]
    [HttpPost]
    public async Task<IActionResult> ReviewUser(string userId, string decision, string note)
    {
        var user = await _userManager.FindByIdAsync(userId);
        var currentUser = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (decision == "Approve")
        {
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
            TempData["Success"] = "Onaylandı.";
        }
        else
        {
            // --- GEÇMİŞE KAYDET ---
            var history = new ApplicantHistory
            {
                Email = user.Email!,
                FullName = user.FullName,
                Result = "Reddedildi",
                ManagerName = currentUser?.FullName ?? "Sistem",
                ManagerNote = note,
                ActionDate = DateTime.UtcNow.AddHours(3)
            };
            _context.ApplicantHistories.Add(history);
            await _context.SaveChangesAsync();
            // ----------------------

            await _userManager.DeleteAsync(user);
            TempData["Error"] = "Reddedildi.";
        }
        return RedirectToAction(nameof(PendingUsers));
    }

    // 9. PERSONEL LİSTESİ
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> PersonnelList() => View(await _userManager.GetUsersInRoleAsync("Personel"));

    // 10. KİLİTLE / AÇ
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> ToggleUserStatus(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                user.LockoutEnd = null;
                user.DeactivatedDate = null;
                TempData["Success"] = "Kilit açıldı.";
            }
            else
            {
                user.LockoutEnd = DateTime.UtcNow.AddYears(100);
                user.DeactivatedDate = DateTime.UtcNow;
                TempData["Error"] = "Kilitlendi.";
            }
            await _userManager.UpdateAsync(user);
        }
        return RedirectToAction(nameof(PersonnelList));
    }

    // 11. KALICI SİLME (SADECE ADMİN)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> HardDeleteUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            try
            {
                var userLogs = _context.RequestLogs.Where(l => l.ProcessorId == userId);
                _context.RequestLogs.RemoveRange(userLogs);

                var userRequests = _context.WorkflowRequests.Where(r => r.RequesterId == userId);
                _context.WorkflowRequests.RemoveRange(userRequests);

                var assignments = _context.InventoryAssignments.Where(a => a.AssignedToUserId == userId);
                _context.InventoryAssignments.RemoveRange(assignments);

                var ownedItems = _context.InventoryItems.Where(i => i.CurrentOwnerId == userId);
                foreach (var item in ownedItems) item.CurrentOwnerId = null;

                await _context.SaveChangesAsync();
                await _userManager.DeleteAsync(user);

                TempData["Success"] = "Kullanıcı ve tüm geçmişi silindi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Hata: " + ex.Message;
            }
        }
        return RedirectToAction(nameof(PersonnelList));
    }

    // 12. TERFİ ETTİR
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PromoteToManager(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            await _userManager.RemoveFromRoleAsync(user, "Personel");
            await _userManager.AddToRoleAsync(user, "Manager");
            TempData["Success"] = $"{user.FullName} MÜDÜR oldu.";
        }
        return RedirectToAction(nameof(PersonnelList));
    }

    // 13. ŞİFRE DEĞİŞTİRME
    [Authorize]
    public IActionResult ChangePassword() => View();

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login");
        var res = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (res.Succeeded) { await _signInManager.RefreshSignInAsync(user); TempData["Success"] = "Şifre değişti."; return RedirectToAction("Index", "Home"); }
        return View(model);
    }

    // ==========================================
    // 14. KULLANICI VE ROL YÖNETİMİ (SADECE ADMIN)
    // ==========================================

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UserList()
    {
        var users = _userManager.Users.ToList();
        var userRolesViewModel = new List<UserRoleViewModel>();

        foreach (var user in users)
        {
            // Kullanıcının rolünü bul
            var roles = await _userManager.GetRolesAsync(user);

            userRolesViewModel.Add(new UserRoleViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                CurrentRole = roles.FirstOrDefault() ?? "Rol Yok" // İlk rolü al
            });
        }

        return View(userRolesViewModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> ChangeRole(string userId, string newRole)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        // 1. Mevcut tüm rolleri kaldır
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        // 2. Yeni rolü ekle
        await _userManager.AddToRoleAsync(user, newRole);

        TempData["Success"] = $"{user.FullName} kullanıcısının rolü '{newRole}' olarak değiştirildi.";
        return RedirectToAction(nameof(UserList));
    }

    public IActionResult AccessDenied() => View();
}