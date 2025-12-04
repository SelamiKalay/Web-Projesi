using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkFlowBasic.Data;
using WorkFlowBasic.Models;
using WorkFlowBasic.Services;

namespace WorkFlowBasic.Controllers;

[Authorize]
public class TaskController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly NotificationService _notificationService;

    public TaskController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService)
    {
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
    }

    // 1. GÖREVLERİM (PERSONEL)
    public async Task<IActionResult> MyTasks()
    {
        var user = await _userManager.GetUserAsync(User);
        var tasks = await _context.WorkTasks
            .Include(t => t.AssignedBy)
            .Where(t => t.AssignedToId == user.Id)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();
        return View(tasks);
    }

    // 2. ATADIĞIM GÖREVLER (MÜDÜR)
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> AssignedTasks()
    {
        var user = await _userManager.GetUserAsync(User);
        var tasks = await _context.WorkTasks
            .Include(t => t.AssignedTo)
            .Where(t => t.AssignedById == user.Id)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();
        return View(tasks);
    }

    // 3. YENİ GÖREV ATA (GET)
    [Authorize(Roles = "Manager,Admin")]
    public IActionResult Create()
    {
        ViewBag.Departments = new List<string> { "IT / Yazılım", "Satış", "Pazarlama", "Muhasebe", "İnsan Kaynakları", "Genel" };
        return View();
    }

    // AJAX Helper
    [HttpGet]
    public async Task<IActionResult> GetUsersByDepartment(string department)
    {
        var users = await _userManager.Users
            .Where(u => u.Department == department)
            .Select(u => new { id = u.Id, name = u.FullName })
            .ToListAsync();
        return Json(users);
    }

    // 4. YENİ GÖREV ATA (POST)
    [Authorize(Roles = "Manager,Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(WorkTask model, IFormFile? taskFile)
    {
        var user = await _userManager.GetUserAsync(User);

        ModelState.Remove("AssignedBy");
        ModelState.Remove("AssignedById");
        ModelState.Remove("AssignedTo");

        if (ModelState.IsValid)
        {
            if (taskFile != null && taskFile.Length > 0)
            {
                var extension = Path.GetExtension(taskFile.FileName);
                var newFileName = $"Task_{Guid.NewGuid()}{extension}";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "task_files");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                using (var stream = new FileStream(Path.Combine(path, newFileName), FileMode.Create))
                {
                    await taskFile.CopyToAsync(stream);
                }
                model.AttachmentPath = "/task_files/" + newFileName;
            }

            model.AssignedById = user.Id;
            model.CreatedDate = DateTime.UtcNow.AddHours(3);
            model.Status = Models.TaskStatus.Assigned;

            _context.WorkTasks.Add(model);
            await _context.SaveChangesAsync();

            await _notificationService.SendNotificationAsync(
                model.AssignedToId, "Yeni Görev", $"{user.FullName} size görev atadı: {model.Title}", "/Task/MyTasks");

            TempData["Success"] = "Görev ve dosya başarıyla gönderildi.";
            return RedirectToAction(nameof(AssignedTasks));
        }

        ViewBag.Departments = new List<string> { "IT / Yazılım", "Satış", "Pazarlama", "Muhasebe", "İnsan Kaynakları", "Genel" };
        return View(model);
    }

    // 5. GÖREVİ TAMAMLA (PERSONEL)
    [HttpPost]
    public async Task<IActionResult> CompleteTask(int id, string note, IFormFile? completionFile)
    {
        var task = await _context.WorkTasks.FindAsync(id);
        var user = await _userManager.GetUserAsync(User);

        if (task != null && task.AssignedToId == user.Id && (task.Status == Models.TaskStatus.Assigned || task.Status == Models.TaskStatus.RevisionRequested))
        {
            if (completionFile != null && completionFile.Length > 0)
            {
                var extension = Path.GetExtension(completionFile.FileName);
                var newFileName = $"Complete_{task.Id}_{Guid.NewGuid()}{extension}";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "task_files");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                using (var stream = new FileStream(Path.Combine(path, newFileName), FileMode.Create))
                {
                    await completionFile.CopyToAsync(stream);
                }
                task.CompletionAttachmentPath = "/task_files/" + newFileName;
            }

            task.Status = Models.TaskStatus.Completed;
            task.CompletionNote = note;
            await _context.SaveChangesAsync();

            await _notificationService.SendNotificationAsync(
                task.AssignedById, "Görev Teslim Edildi", $"{user.FullName} görevi tamamladı.", "/Task/ReviewTask/" + task.Id);

            TempData["Success"] = "Görev teslim edildi.";
        }
        return RedirectToAction(nameof(MyTasks));
    }

    // 6. GÖREV İNCELEME SAYFASI
    [Authorize(Roles = "Manager,Admin")]
    [HttpGet]
    public async Task<IActionResult> ReviewTask(int id)
    {
        var task = await _context.WorkTasks
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null) return NotFound();
        return View(task);
    }

    // 7. GÖREV KARAR İŞLEMİ (MÜDÜR - REVIZE VE TARIH GUNCELLEME)
    [Authorize(Roles = "Manager,Admin")]
    [HttpPost]
    public async Task<IActionResult> ProcessTask(int id, string decision, string managerNote, DateTime? newDueDate)
    {
        var task = await _context.WorkTasks.FindAsync(id);

        if (task != null)
        {
            if (decision == "Approve")
            {
                task.Status = Models.TaskStatus.Verified;
                await _notificationService.SendNotificationAsync(task.AssignedToId, "Görev Onaylandı ✅", "Göreviniz onaylandı.", "/Task/MyTasks");
                TempData["Success"] = "Görev onaylandı.";
            }
            else if (decision == "Revise")
            {
                task.Status = Models.TaskStatus.RevisionRequested;

                // --- YENİ: TARİH GÜNCELLEME ---
                if (newDueDate.HasValue)
                {
                    task.DueDate = newDueDate.Value;
                    task.IsOverdueNotified = false; // Gecikme uyarısını sıfırla
                    managerNote += $" (Yeni Tarih: {task.DueDate.ToShortDateString()})";
                }
                // ------------------------------

                task.Description = $"[REVİZE: {managerNote}] \n " + task.Description;

                await _notificationService.SendNotificationAsync(task.AssignedToId, "Revize İsteniyor ⚠️", $"Düzeltme ve ek süre verildi: {managerNote}", "/Task/MyTasks");
                TempData["Warning"] = "Revize isteği ve yeni tarih gönderildi.";
            }
            else if (decision == "Reject")
            {
                task.Status = Models.TaskStatus.Assigned;
                task.CompletionAttachmentPath = null;
                await _notificationService.SendNotificationAsync(task.AssignedToId, "Görev Reddedildi ❌", "Göreviniz kabul edilmedi.", "/Task/MyTasks");
                TempData["Error"] = "Görev reddedildi.";
            }

            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(AssignedTasks));
    }
}