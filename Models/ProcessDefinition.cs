using System.ComponentModel.DataAnnotations;

namespace WorkFlowBasic.Models;

public class ProcessDefinition
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public required string ProcessName { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public string? FormSchema { get; set; }
}