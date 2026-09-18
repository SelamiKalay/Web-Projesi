using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkFlowBasic.Data;
using WorkFlowBasic.Models;

namespace WorkFlowBasic.Controllers;

// Sadece yetkili kullanıcılar erişebilir. Kullanıcı nesneleri doğrudan döndürülmez;
// IdentityUser içindeki PasswordHash, SecurityStamp vb. alanlar sızmasın diye
// yalnızca gerekli alanlar seçilir.
[Authorize(Roles = "Admin,Manager")]
[Route("api/[controller]")]
[ApiController]
public class WorkflowApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public WorkflowApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    // 1. TÜM TALEPLERİ GETİR (JSON)
    // Adres: GET /api/WorkflowApi
    [HttpGet]
    public async Task<IActionResult> GetRequests()
    {
        var requests = await _context.WorkflowRequests
            .OrderByDescending(r => r.CreatedDate)
            .Select(r => new
            {
                r.Id,
                r.RequestTitle,
                r.Description,
                r.Amount,
                r.Status,
                r.CreatedDate,
                r.CompletedDate,
                r.StartDate,
                r.EndDate,
                Process = r.ProcessDefinition == null ? null : r.ProcessDefinition.ProcessName,
                Requester = r.Requester == null ? null : new { r.Requester.Id, r.Requester.FirstName, r.Requester.LastName, r.Requester.Department },
                CurrentApprover = r.CurrentApprover == null ? null : new { r.CurrentApprover.Id, r.CurrentApprover.FirstName, r.CurrentApprover.LastName }
            })
            .ToListAsync();

        return Ok(requests);
    }

    // 2. TEK BİR TALEBİ GETİR (ID ile)
    // Adres: GET /api/WorkflowApi/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRequest(int id)
    {
        var request = await _context.WorkflowRequests
            .Where(r => r.Id == id)
            .Select(r => new
            {
                r.Id,
                r.RequestTitle,
                r.Description,
                r.Amount,
                r.Status,
                r.CreatedDate,
                r.CompletedDate,
                r.StartDate,
                r.EndDate,
                r.FormData,
                Process = r.ProcessDefinition == null ? null : r.ProcessDefinition.ProcessName,
                Requester = r.Requester == null ? null : new { r.Requester.Id, r.Requester.FirstName, r.Requester.LastName, r.Requester.Department },
                Logs = r.Logs.OrderBy(l => l.ActionDate).Select(l => new { l.Action, l.Comments, l.ActionDate, l.ProcessorId })
            })
            .FirstOrDefaultAsync();

        if (request == null)
        {
            return NotFound("Talep bulunamadı.");
        }

        return Ok(request);
    }

    // 3. İSTATİSTİKLERİ GETİR (Dashboard verisi)
    // Adres: GET /api/WorkflowApi/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = new
        {
            Total = await _context.WorkflowRequests.CountAsync(),
            Pending = await _context.WorkflowRequests.CountAsync(x => x.Status == RequestStatus.Pending),
            Approved = await _context.WorkflowRequests.CountAsync(x => x.Status == RequestStatus.Approved),
            Rejected = await _context.WorkflowRequests.CountAsync(x => x.Status == RequestStatus.Rejected)
        };

        return Ok(stats);
    }
}
