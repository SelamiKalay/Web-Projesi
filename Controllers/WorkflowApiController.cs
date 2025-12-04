using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkFlowBasic.Data;
using WorkFlowBasic.Models;

namespace WorkFlowBasic.Controllers;

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
    public async Task<ActionResult<IEnumerable<WorkflowRequest>>> GetRequests()
    {
        // İlişkili verileri de (Include) getiriyoruz
        return await _context.WorkflowRequests
            .Include(r => r.Requester)
            .Include(r => r.ProcessDefinition)
            .Include(r => r.CurrentApprover)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();
    }

    // 2. TEK BİR TALEBİ GETİR (ID ile)
    // Adres: GET /api/WorkflowApi/5
    [HttpGet("{id}")]
    public async Task<ActionResult<WorkflowRequest>> GetRequest(int id)
    {
        var request = await _context.WorkflowRequests
            .Include(r => r.Requester)
            .Include(r => r.ProcessDefinition)
            .Include(r => r.Logs)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
        {
            return NotFound("Talep bulunamadı.");
        }

        return request;
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