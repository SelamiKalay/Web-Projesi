using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkFlowBasic.Data;
using WorkFlowBasic.Models;

namespace WorkFlowBasic.Services;

public class CleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CleanupBackgroundService> _logger;

    public CleanupBackgroundService(IServiceProvider serviceProvider, ILogger<CleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Temizlik ve Kontrol Robotu Başlatıldı...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    
                    // --- ORTAK ZAMAN ---
                    var now = DateTime.UtcNow.AddHours(3);

                    // --- GÖREV 1: ESKİ TALEPLERİ SİL (30 GÜN) ---
                    var requestCutOffDate = now.AddDays(-30);
                    var oldRequests = context.WorkflowRequests
                        .Where(r => r.Status != RequestStatus.Pending 
                                 && r.CompletedDate != null 
                                 && r.CompletedDate < requestCutOffDate)
                        .ToList();

                    if (oldRequests.Any())
                    {
                        context.WorkflowRequests.RemoveRange(oldRequests);
                        _logger.LogInformation($"{oldRequests.Count} adet eski talep silindi.");
                    }

                    // --- GÖREV 2: PASİF KULLANICILARI SİL (30 GÜN) ---
                    var usersToDelete = await userManager.Users
                        .Where(u => u.DeactivatedDate != null && u.DeactivatedDate < requestCutOffDate)
                        .ToListAsync();

                    if (usersToDelete.Any())
                    {
                        foreach (var user in usersToDelete)
                        {
                            await userManager.DeleteAsync(user);
                            _logger.LogInformation($"Pasif kullanıcı silindi: {user.Email}");
                        }
                    }

                    // --- GÖREV 3: ESKİ BİLDİRİMLERİ SİL (1 GÜN) ---
                    var notificationCutOffDate = now.AddDays(-1);
                    var oldNotifications = context.Notifications
                        .Where(n => n.CreatedDate < notificationCutOffDate)
                        .ToList();

                    if (oldNotifications.Any())
                    {
                        context.Notifications.RemoveRange(oldNotifications);
                        _logger.LogInformation($"{oldNotifications.Count} adet eski bildirim temizlendi.");
                    }

                    // --- GÖREV 4: GECİKEN İŞLERİ MÜDÜRE BİLDİR (YENİ EKLENDİ) ---
                    var overdueTasks = await context.WorkTasks
                        .Include(t => t.AssignedTo)
                        .Where(t => t.Status == WorkFlowBasic.Models.TaskStatus.Assigned
                                 && t.DueDate < now                       
                                 && !t.IsOverdueNotified)                 
                        .ToListAsync();

                    if (overdueTasks.Any())
                    {
                        foreach (var task in overdueTasks)
                        {
                            var notif = new Notification
                            {
                                UserId = task.AssignedById, // Müdüre
                                Title = "Gecikme Uyarısı ⏰",
                                Message = $"{task.AssignedTo?.FullName}, '{task.Title}' görevini zamanında teslim etmedi.",
                                LinkUrl = $"/Task/ReviewTask/{task.Id}",
                                CreatedDate = now,
                                IsRead = false
                            };
                            
                            context.Notifications.Add(notif);
                            task.IsOverdueNotified = true; // Tekrar bildirim gitmesin
                        }
                        _logger.LogInformation($"{overdueTasks.Count} adet gecikmiş görev bildirildi.");
                    }
                    // -----------------------------------------------------------

                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Arka plan servisinde hata oluştu.");
            }

            // 24 Saat Bekle
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}