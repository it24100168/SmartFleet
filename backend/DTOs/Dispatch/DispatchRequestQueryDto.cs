using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.DTOs.Dispatch;

/// <summary>
/// Query filters, sorting, and pagination parameters for dispatch request listing.
/// </summary>
public class DispatchRequestQueryDto
{
    public string? Search { get; set; }
    public DispatchRequestStatus? Status { get; set; }
    public string? Zone { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
