using System.ComponentModel.DataAnnotations;

namespace WorkFlowBasic.Models;

public class InventoryItem
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public required string ItemName { get; set; } // Örn: MacBook Pro M2

    [StringLength(50)]
    public string? SerialNumber { get; set; } // Örn: C02XYZ...

    [StringLength(50)]
    public string? Category { get; set; } // Bilgisayar, Telefon, Araç

    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

    // Şu an kimde? (Boşsa depodadır)
    public string? CurrentOwnerId { get; set; }
}