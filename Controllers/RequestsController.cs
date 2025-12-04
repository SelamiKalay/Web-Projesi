using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WorkFlowBasic.Data;
using WorkFlowBasic.Models;
using WorkFlowBasic.Services;

namespace WorkFlowBasic.Controllers;

[Authorize]
public class RequestsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly NotificationService _notificationService; // <-- YENİ

    public RequestsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        NotificationService notificationService) // <-- Constructor'a eklendi
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
        _notificationService = notificationService;
    }

    // 1. TALEPLERİM LİSTESİ
    public async Task<IActionResult> MyRequests()
    {
        var userId = _userManager.GetUserId(User);
        var requests = await _context.WorkflowRequests
            .Include(r => r.ProcessDefinition)
            .Include(r => r.CurrentApprover)
            .Where(r => r.RequesterId == userId)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();

        return View(requests);
    }

    // 2. YENİ TALEP SAYFASI
    [HttpGet]
    [Authorize(Roles = "Personel")]
    public IActionResult Create()
    {
        ViewBag.ProcessList = new SelectList(_context.ProcessDefinitions.Where(p => p.IsActive), "Id", "ProcessName");
        return View();
    }

    // 3. TALEP KAYDETME
    [HttpPost]
    [Authorize(Roles = "Personel")]
    public async Task<IActionResult> Create(WorkflowRequest model, IFormFile? attachment)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        ModelState.Remove("RequesterId");
        ModelState.Remove("CurrentApproverId");
        ModelState.Remove("Requester");
        ModelState.Remove("ProcessDefinition");

        if (ModelState.IsValid)
        {
            // DOSYA YÜKLEME
            if (attachment != null && attachment.Length > 0)
            {
                var extension = Path.GetExtension(attachment.FileName);
                var newFileName = Guid.NewGuid().ToString() + extension;
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                using (var stream = new FileStream(Path.Combine(uploadPath, newFileName), FileMode.Create))
                {
                    await attachment.CopyToAsync(stream);
                }
                model.AttachmentPath = "/uploads/" + newFileName;
            }

            // VERİ DOLDURMA
            model.RequesterId = user.Id;
            model.CreatedDate = DateTime.UtcNow.AddHours(3);

            // Yöneticiye Ata
            if (user.ManagerId != null)
            {
                model.CurrentApproverId = user.ManagerId;
                model.Status = RequestStatus.Pending;
            }
            else
            {
                model.Status = RequestStatus.Approved;
                model.CompletedDate = DateTime.UtcNow.AddHours(3);
                model.CurrentApproverId = user.Id;
            }

            _context.WorkflowRequests.Add(model);
            await _context.SaveChangesAsync();

            _context.RequestLogs.Add(new RequestLog
            {
                WorkflowRequestId = model.Id,
                ProcessorId = user.Id,
                Action = LogAction.Created,
                Comments = "Talep oluşturuldu.",
                ActionDate = DateTime.UtcNow.AddHours(3)
            });
            await _context.SaveChangesAsync();

            // --- BİLDİRİM GÖNDER (MÜDÜRE) ---
            if (model.CurrentApproverId != null && model.Status == RequestStatus.Pending)
            {
                // Müdüre Bildirim At: "Yeni bir talep var"
                await _notificationService.SendNotificationAsync(
                    model.CurrentApproverId,
                    "Yeni Talep",
                    $"{user.FullName} yeni bir talep oluşturdu.",
                    "/Approvals/Index"
                );
            }
            // --------------------------------

            TempData["Success"] = "Talebiniz başarıyla oluşturuldu ve yöneticiye iletildi.";
            return RedirectToAction(nameof(MyRequests));
        }

        ViewBag.ProcessList = new SelectList(_context.ProcessDefinitions.Where(p => p.IsActive), "Id", "ProcessName");
        return View(model);
    }

    // 4. AJAX HELPER
    [HttpGet]
    public async Task<IActionResult> GetProcessSchema(int id)
    {
        var process = await _context.ProcessDefinitions.FindAsync(id);
        if (process == null || string.IsNullOrEmpty(process.FormSchema)) return Ok(null);
        return Ok(process.FormSchema);
    }

    // 5. DETAY GÖRÜNTÜLEME (Personel İçin)
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var request = await _context.WorkflowRequests
            .Include(r => r.Requester)
            .Include(r => r.ProcessDefinition)
            .Include(r => r.Logs).ThenInclude(l => l.Processor)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null) return NotFound();
        if (!User.IsInRole("Manager") && !User.IsInRole("Admin") && request.RequesterId != user.Id) return Unauthorized();

        return View(request);
    }
}