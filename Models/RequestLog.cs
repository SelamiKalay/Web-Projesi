using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkFlowBasic.Models;

public class RequestLog
{
    public int Id { get; set; }

    public int WorkflowRequestId { get; set; }
    public WorkflowRequest? WorkflowRequest { get; set; }

    // İşlemi yapan kişi
    [Required]
    public required string ProcessorId { get; set; }

    [ForeignKey(nameof(ProcessorId))]
    public ApplicationUser? Processor { get; set; }

    public LogAction Action { get; set; }

    public string? Comments { get; set; }

    public DateTime ActionDate { get; set; } = DateTime.UtcNow;
}