using System.ComponentModel.DataAnnotations;

namespace WorkFlowBasic.Models;

public class ApplicantHistory
{
    public int Id { get; set; }

    [Required]
    public required string Email { get; set; } // Kişiyi emailinden tanıyacağız
    public string FullName { get; set; } = "";

    public string Result { get; set; } = "Rejected"; // Rejected, Approved
    public string? ManagerNote { get; set; } // Neden reddedildi?
    public string ManagerName { get; set; } = ""; // Kim reddetti?

    public DateTime ActionDate { get; set; } = DateTime.UtcNow.AddHours(3);
}