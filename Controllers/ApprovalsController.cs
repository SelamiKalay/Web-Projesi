using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WorkFlowBasic.Data;
using WorkFlowBasic.Hubs;
using WorkFlowBasic.Models;
using WorkFlowBasic.Services; // <-- Eklendi

namespace WorkFlowBasic.Controllers;

[Authorize(Roles = "Manager,Admin")]
public class ApprovalsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHubContext<GeneralHub> _hubContext;
    private readonly NotificationService _notificationService; // <-- Servis

    public ApprovalsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IHubContext<GeneralHub> hubContext,
        NotificationService notificationService)
    {
        _context = context;
        _userManager = userManager;
        _hubContext = hubContext;
        _notificationService = notificationService;
    }

    // 1. ONAYLAR LİSTESİ
    public async Task<IActionResult> Index(string searchString, RequestStatus? statusFilter)
    {
        var user = await _userManager.GetUserAsync(User);
        var query = _context.WorkflowRequests
            .Include(r => r.Requester)
            .Include(r => r.ProcessDefinition)
            .Where(r => r.CurrentApproverId == user.Id);

        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(r => r.Requester.FirstName.Contains(searchString)
                                  || r.Requester.LastName.Contains(searchString)
                                  || r.RequestTitle.Contains(searchString));
        }

        if (statusFilter.HasValue) query = query.Where(r => r.Status == statusFilter.Value);
        else query = query.Where(r => r.Status == RequestStatus.Pending);

        var requests = await query.OrderByDescending(r => r.CreatedDate).ToListAsync();
        ViewBag.CurrentFilter = searchString;
        ViewBag.CurrentStatus = statusFilter;

        return View(requests);
    }

    // 2. DETAY SAYFASI
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var request = await _context.WorkflowRequests
            .Include(r => r.Requester)
            .Include(r => r.ProcessDefinition)
            .Include(r => r.Logs).ThenInclude(l => l.Processor)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null) return NotFound();
        return View(request);
    }

    // 3. İŞLEMİ KAYDET (TEKLİ)
    [HttpPost]
    public async Task<IActionResult> Process(int requestId, string decision, string comments)
    {
        var user = await _userManager.GetUserAsync(User);
        var request = await _context.WorkflowRequests.FindAsync(requestId);

        if (request == null || request.CurrentApproverId != user.Id) return BadRequest("Yetkisiz işlem.");

        if (decision == "Approve") { request.Status = RequestStatus.Approved; request.CompletedDate = DateTime.UtcNow.AddHours(3); }
        else if (decision == "Reject") { request.Status = RequestStatus.Rejected; request.CompletedDate = DateTime.UtcNow.AddHours(3); }

        _context.RequestLogs.Add(new RequestLog
        {
            WorkflowRequestId = request.Id,
            ProcessorId = user.Id,
            ActionDate = DateTime.UtcNow.AddHours(3),
            Comments = comments,
            Action = decision == "Approve" ? LogAction.Approved : LogAction.Rejected
        });
        await _context.SaveChangesAsync();

        // --- BİLDİRİM SİSTEMİ (ZİL + TOAST) ---
        string title = decision == "Approve" ? "Talebiniz Onaylandı ✅" : "Talebiniz Reddedildi ❌";
        string message = $"'{request.RequestTitle}' konulu talebiniz sonuçlandı.";
        string url = $"/Requests/Details/{request.Id}";

        // 1. Veritabanına kalıcı bildirim at (Zil için)
        await _notificationService.SendNotificationAsync(request.RequesterId, title, message, url);

        // 2. Ekrana canlı Toast at (SignalR)
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", message);
        // -------------------------------------

        return RedirectToAction(nameof(Index));
    }

    // 4. TOPLU İŞLEM
    [HttpPost]
    public async Task<IActionResult> ProcessBulk(List<int> selectedIds, string decision)
    {
        var user = await _userManager.GetUserAsync(User);
        if (selectedIds == null || !selectedIds.Any()) return RedirectToAction(nameof(Index));

        var requestsToProcess = await _context.WorkflowRequests
            .Where(r => selectedIds.Contains(r.Id) && r.CurrentApproverId == user.Id && r.Status == RequestStatus.Pending)
            .ToListAsync();

        foreach (var req in requestsToProcess)
        {
            if (decision == "Approve") { req.Status = RequestStatus.Approved; req.CompletedDate = DateTime.UtcNow.AddHours(3); }
            else if (decision == "Reject") { req.Status = RequestStatus.Rejected; req.CompletedDate = DateTime.UtcNow.AddHours(3); }

            _context.RequestLogs.Add(new RequestLog
            {
                WorkflowRequestId = req.Id,
                ProcessorId = user.Id,
                ActionDate = DateTime.UtcNow.AddHours(3),
                Action = decision == "Approve" ? LogAction.Approved : LogAction.Rejected,
                Comments = "Toplu işlem."
            });

            // Her birine bildirim at
            await _notificationService.SendNotificationAsync(req.RequesterId, "Talep Sonuçlandı", $"'{req.RequestTitle}' talebiniz işleme alındı.", $"/Requests/Details/{req.Id}");
        }

        await _context.SaveChangesAsync();
        // Toplu işlemde de canlı sinyal gönderelim
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Toplu işlemler tamamlandı, sayfa yenileniyor...");

        return RedirectToAction(nameof(Index));
    }

    // ... (RejectedIndex ve Recover metodları aynen kalacak) ...
    public async Task<IActionResult> RejectedIndex()
    {
        var user = await _userManager.GetUserAsync(User);
        var rejectedRequests = await _context.WorkflowRequests
            .Include(r => r.Requester)
            .Include(r => r.ProcessDefinition)
            .Where(r => r.CurrentApproverId == user.Id && r.Status == RequestStatus.Rejected)
            .OrderByDescending(r => r.CompletedDate)
            .ToListAsync();
        return View(rejectedRequests);
    }

    [HttpPost]
    public async Task<IActionResult> Recover(int id)
    {
        var request = await _context.WorkflowRequests.FindAsync(id);
        var user = await _userManager.GetUserAsync(User);

        if (request != null && request.CurrentApproverId == user.Id && request.Status == RequestStatus.Rejected)
        {
            request.Status = RequestStatus.Pending;
            request.CompletedDate = null;
            _context.RequestLogs.Add(new RequestLog { WorkflowRequestId = request.Id, ProcessorId = user.Id, Action = LogAction.RequestRevision, Comments = "Geri alındı.", ActionDate = DateTime.UtcNow.AddHours(3) });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Talep geri alındı.";
        }
        return RedirectToAction(nameof(RejectedIndex));
    }
}