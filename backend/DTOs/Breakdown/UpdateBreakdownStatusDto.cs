using System.ComponentModel.DataAnnotations;
using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.DTOs.Breakdown;

public class UpdateBreakdownStatusDto
{
    [Required(ErrorMessage = "Status is required.")]
    public BreakdownStatus Status { get; set; }
}
