using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkFlowBasic.Models;

public enum TaskStatus
{
    Assigned = 0,       // Atandı / Yapılacak
    Completed = 1,      // Personel Tamamladı
    Verified = 2,        // Yönetici Onayladı (Bitti)
    RevisionRequested = 3

}

public class WorkTask
{
    public int Id { get; set; }

    [Required]
    public required string Title { get; set; } // Görev Başlığı

    [Required]
    public required string Description { get; set; } // Detay

    // Kime Atandı? (Personel)
    [Required]
    public required string AssignedToId { get; set; }
    [ForeignKey(nameof(AssignedToId))]
    public ApplicationUser? AssignedTo { get; set; }

    // Kim Atadı? (Müdür)
    public required string AssignedById { get; set; }
    [ForeignKey(nameof(AssignedById))]
    public ApplicationUser? AssignedBy { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow.AddHours(3);

    public TaskStatus Status { get; set; } = TaskStatus.Assigned;

    // Personel notu (Tamamlarken yazacağı)
    public string? CompletionNote { get; set; }
    public string? AttachmentPath { get; set; }
    public string? CompletionAttachmentPath { get; set; }
    public bool IsOverdueNotified { get; set; } = false;
}