using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkFlowBasic.Data;
using WorkFlowBasic.Models;

namespace WorkFlowBasic.Controllers;

[Authorize]
public class CalendarController : Controller
{
    private readonly ApplicationDbContext _context;

    public CalendarController(ApplicationDbContext context)
    {
        _context = context;
    }

    // 1. TAKVİM SAYFASI (Görünüm)
    public IActionResult Index()
    {
        return View();
    }

    // 2. VERİLERİ JSON OLARAK GETİR (API)
    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        // Kullanıcının rolünü kontrol et (Yönetici mi?)
        bool isManager = User.IsInRole("Manager") || User.IsInRole("Admin");

        // Sadece Onaylanmış ve Tarihi olan talepleri getir
        var events = await _context.WorkflowRequests
            .Include(r => r.Requester) // Kimin izni olduğunu bilmek için
            .Where(r => r.Status == RequestStatus.Approved && r.StartDate != null && r.EndDate != null)
            .Select(e => new
            {
                id = e.Id,
                title = $"{e.Requester.FirstName} {e.Requester.LastName} - {e.RequestTitle}",
                start = e.StartDate.Value.ToString("yyyy-MM-dd"),
                end = e.EndDate.Value.AddDays(1).ToString("yyyy-MM-dd"), // FullCalendar bitiş gününü dahil etmez, o yüzden +1 gün ekliyoruz
                color = "#28a745", // Yeşil renk (Onaylı olduğu için)

                // --- DİNAMİK URL (ÇÖZÜM BURASI) ---
                // Yönetici ise -> Yönetim Detayına git
                // Personel ise -> Kendi Detayına git
                url = isManager ? $"/Approvals/Details/{e.Id}" : $"/Requests/Details/{e.Id}"
                // ----------------------------------
            })
            .ToListAsync();

        return Json(events);
    }
}