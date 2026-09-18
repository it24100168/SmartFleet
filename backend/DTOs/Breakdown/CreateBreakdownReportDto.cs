using System.ComponentModel.DataAnnotations;

namespace SmartFleet.Backend.DTOs.Breakdown;

public class CreateBreakdownReportDto
{
    public Guid? RoverId { get; set; }

    [Required(ErrorMessage = "Symptom category is required.")]
    [StringLength(64, ErrorMessage = "Symptom category cannot exceed 64 characters.")]
    public string SymptomCategory { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string Description { get; set; } = string.Empty;

    [StringLength(32, ErrorMessage = "Error code cannot exceed 32 characters.")]
    public string? ErrorCode { get; set; }

    public IFormFile? Photo { get; set; }
}
