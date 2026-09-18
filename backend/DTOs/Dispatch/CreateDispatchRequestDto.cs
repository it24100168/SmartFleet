using System.ComponentModel.DataAnnotations;

namespace SmartFleet.Backend.DTOs.Dispatch;

/// <summary>
/// Request payload for creating a new dispatch order.
/// </summary>
public class CreateDispatchRequestDto
{
    [Required]
    [MaxLength(128)]
    public string SourceZone { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string DestinationZone { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string CargoType { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string Priority { get; set; } = "Medium";

    [Required]
    public DateTime PreferredTimeWindow { get; set; }

    /// <summary>
    /// Optional GPS latitude captured from mobile geolocator.
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Optional GPS longitude captured from mobile geolocator.
    /// </summary>
    public double? Longitude { get; set; }
}
