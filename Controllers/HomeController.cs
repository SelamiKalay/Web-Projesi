using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;
using WorkFlowBasic.Data;
using WorkFlowBasic.Models;

namespace WorkFlowBasic.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    // 1. ANA SAYFA
    public IActionResult Index()
    {
        IQueryable<WorkflowRequest> query = _context.WorkflowRequests;

        if (User.IsInRole("Personel"))
        {
            var currentUser = _context.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (currentUser != null)
            {
                query = query.Where(r => r.RequesterId == currentUser.Id);
            }
        }

        ViewBag.TotalCount = query.Count();
        ViewBag.PendingCount = query.Count(x => x.Status == RequestStatus.Pending);
        ViewBag.ApprovedCount = query.Count(x => x.Status == RequestStatus.Approved);
        ViewBag.RejectedCount = query.Count(x => x.Status == RequestStatus.Rejected);

        return View();
    }

    // 2. DÝL DEÐÝÞTÝRME METODU (GÜNCELLENMÝÞ HALÝ)
    [AllowAnonymous]
    [HttpPost]
    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
        );

        // Eðer dönüþ URL'i boþsa ana sayfaya at (Hata vermemesi için)
        return LocalRedirect(returnUrl ?? "/");
    }

    // 3. SÝSTEMÝ SIFIRLAMA (ADMIN)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ResetSystem()
    {
        _context.RequestLogs.RemoveRange(_context.RequestLogs);
        _context.WorkflowRequests.RemoveRange(_context.WorkflowRequests);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Sistem sýfýrlandý.";
        return RedirectToAction("Index");
    }

    // 4. ESKÝLERÝ TEMÝZLE (ADMIN)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CleanOldData()
    {
        var cutOffDate = DateTime.UtcNow.AddDays(-30);
        var oldRequests = _context.WorkflowRequests
            .Where(r => r.Status != RequestStatus.Pending
                     && r.CompletedDate != null
                     && r.CompletedDate < cutOffDate);

        int count = oldRequests.Count();

        if (count > 0)
        {
            _context.WorkflowRequests.RemoveRange(oldRequests);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"{count} eski talep silindi.";
        }
        else
        {
            TempData["Success"] = "Silinecek veri yok.";
        }
        return RedirectToAction("Index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}