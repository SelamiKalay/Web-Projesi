using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkFlowBasic.Models;

public class WorkflowRequest
{
    public int Id { get; set; }

    // Talep sahibi
    [Required]
    public required string RequesterId { get; set; }

    [ForeignKey(nameof(RequesterId))]
    public ApplicationUser? Requester { get; set; }

    // Hangi süreç?
    public int ProcessDefinitionId { get; set; }
    public ProcessDefinition? ProcessDefinition { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Draft;

    // Şu an onaylaması gereken kişi
    public string? CurrentApproverId { get; set; }

    [ForeignKey(nameof(CurrentApproverId))]
    public ApplicationUser? CurrentApprover { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedDate { get; set; }

    public string? RequestTitle { get; set; }
    public string? Description { get; set; }
    public decimal? Amount { get; set; }
    public DateTime? StartDate { get; set; } // İzin Başlangıç
    public DateTime? EndDate { get; set; }   // İzin Bitiş
    public string? AttachmentPath { get; set; } // Dosya yolu (Örn: /uploads/belge.pdf)

    public ICollection<RequestLog> Logs { get; set; } = new List<RequestLog>();

    // ... Diğer özelliklerin altı ...

    // Kullanıcının girdiği ekstra cevaplar (JSON Tutacağız)
    // Örn: {"Bilgisayar No": "NB-1234", "Aciliyet": "5"}
    public string? FormData { get; set; }
}