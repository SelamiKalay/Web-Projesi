using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkFlowBasic.Models;

public class InventoryAssignment
{
    public int Id { get; set; }

    // Hangi Eşya?
    public int InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }

    // Kime Verildi?
    [Required]
    public required string AssignedToUserId { get; set; }

    [ForeignKey(nameof(AssignedToUserId))]
    public ApplicationUser? AssignedToUser { get; set; }

    // Ne Zaman Verildi?
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

    // Personel Kabul Etti mi?
    public bool IsAccepted { get; set; } = false;
    public DateTime? AcceptanceDate { get; set; }

    // İade Tarihi (Eğer geri verdiyse burası dolar)
    public DateTime? ReturnDate { get; set; }

    public string? SignaturePath { get; set; } // Dijital İmza Resminin Yolu
    public bool IsReturnRequested { get; set; } = false;
}