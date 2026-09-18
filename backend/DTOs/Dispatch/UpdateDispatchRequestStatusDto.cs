using System.ComponentModel.DataAnnotations;
using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.DTOs.Dispatch;

/// <summary>
/// Payload for updating the status of a dispatch request.
/// </summary>
public class UpdateDispatchRequestStatusDto
{
    [Required]
    public DispatchRequestStatus Status { get; set; }
}
