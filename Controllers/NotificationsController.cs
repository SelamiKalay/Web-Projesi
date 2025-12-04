using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkFlowBasic.Data;
using WorkFlowBasic.Models;

namespace WorkFlowBasic.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // 1. BİLDİRİMLERİ GETİR (AJAX İÇİN)
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var notifs = await _context.Notifications
            .Where(n => n.UserId == user.Id)
            .OrderByDescending(n => n.CreatedDate)
            .Take(10) // Son 10 bildirim
            .ToListAsync();

        return Json(notifs);
    }

    // 2. OKUNMAMIŞ SAYISINI GETİR
    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Ok(0);

        var count = await _context.Notifications.CountAsync(n => n.UserId == user.Id && !n.IsRead);
        return Ok(count);
    }

    // 3. HEPSİNİ OKUNDU YAP
    [HttpPost]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var user = await _userManager.GetUserAsync(User);
        var unread = await _context.Notifications.Where(n => n.UserId == user.Id && !n.IsRead).ToListAsync();

        foreach (var n in unread) n.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok();
    }
}