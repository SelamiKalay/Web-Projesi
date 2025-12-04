using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WorkFlowBasic.Data;
using WorkFlowBasic.Hubs;
using WorkFlowBasic.Models;

namespace WorkFlowBasic.Services;

public class NotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<GeneralHub> _hubContext;

    public NotificationService(ApplicationDbContext context, IHubContext<GeneralHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task SendNotificationAsync(string userId, string title, string message, string url)
    {
        // 1. Veritabanına Kaydet (Kalıcı olsun)
        var notif = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            LinkUrl = url,
            IsRead = false,
            CreatedDate = DateTime.UtcNow.AddHours(3)
        };

        _context.Notifications.Add(notif);
        await _context.SaveChangesAsync();

        // 2. SignalR ile Canlı Gönder (Zil çalsın)
        // Not: Gerçek projede userId'ye göre group yapılır, şimdilik genel atıyoruz.
        await _hubContext.Clients.All.SendAsync("UpdateNotificationCount");
    }
}