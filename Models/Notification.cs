using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkFlowBasic.Models;

public class Notification
{
    public int Id { get; set; }

    // Kime gidecek?
    [Required]
    public required string UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    public string Title { get; set; } = "Bildirim";
    public string Message { get; set; } = "";
    public string? LinkUrl { get; set; } // Tıklayınca nereye gitsin?

    public bool IsRead { get; set; } = false; // Okundu mu?
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow.AddHours(3);
}