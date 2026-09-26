using SmartFleet.Backend.Agents.MaintenanceMechanicAgent.DTOs;
using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.DTOs.Breakdown;

public class BreakdownReportResponseDto
{
    public Guid Id { get; set; }

    public Guid? RoverId { get; set; }

    public Guid ReportedById { get; set; }

    public string ReportedByName { get; set; } = string.Empty;

    public string ReportedByEmail { get; set; } = string.Empty;

    public string SymptomCategory { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? PhotoUrl { get; set; }

    public string? ErrorCode { get; set; }

    public BreakdownStatus Status { get; set; }

    public MaintenanceMechanicOutput? DiagnosisResult { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
