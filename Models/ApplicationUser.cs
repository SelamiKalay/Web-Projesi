using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkFlowBasic.Models; // BURAYA DİKKAT: Namespace ismi proje adınla aynı olmalı

public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(100)]
    public required string FirstName { get; set; }

    [Required]
    [StringLength(100)]
    public required string LastName { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    [StringLength(100)]
    public string? Department { get; set; }

    public string? ManagerId { get; set; }

    [ForeignKey(nameof(ManagerId))]
    public ApplicationUser? Manager { get; set; }

    public DateTime? DeactivatedDate { get; set; }
    public string? CVFilePath { get; set; }
}