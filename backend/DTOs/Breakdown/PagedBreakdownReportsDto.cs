namespace SmartFleet.Backend.DTOs.Breakdown;

public class PagedBreakdownReportsDto
{
    public IReadOnlyList<BreakdownReportResponseDto> Items { get; set; } = Array.Empty<BreakdownReportResponseDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize));
}
