using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartFleet.Backend.Agents.MaintenanceMechanicAgent;
using SmartFleet.Backend.Agents.MaintenanceMechanicAgent.DTOs;
using SmartFleet.Backend.Data.Repositories;
using SmartFleet.Backend.DTOs.Breakdown;
using SmartFleet.Backend.Models;
using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.Controllers;

[ApiController]
[Route("api/breakdown-reports")]
[Authorize]
public class BreakdownReportsController : ControllerBase
{
    private readonly IBreakdownReportRepository _breakdownReportRepository;
    private readonly IMaintenanceMechanicAgent _mechanicAgent;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<BreakdownReportsController> _logger;

    public BreakdownReportsController(
        IBreakdownReportRepository breakdownReportRepository,
        IMaintenanceMechanicAgent mechanicAgent,
        IWebHostEnvironment environment,
        ILogger<BreakdownReportsController> logger)
    {
        _breakdownReportRepository = breakdownReportRepository;
        _mechanicAgent = mechanicAgent;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// File a new rover breakdown report with optional photo evidence.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(BreakdownReportResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateReport([FromForm] CreateBreakdownReportDto request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { message = "User context is missing or invalid." });
        }

        string? photoUrl = null;

        // Handle multipart photo upload
        if (request.Photo != null && request.Photo.Length > 0)
        {
            var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var uploadsFolder = Path.Combine(webRoot, "uploads", "breakdowns");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var extension = Path.GetExtension(request.Photo.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "Invalid photo format. Allowed types: .jpg, .jpeg, .png, .webp" });
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.Photo.CopyToAsync(stream, cancellationToken);
            }

            photoUrl = $"/uploads/breakdowns/{fileName}";
        }

        var report = new BreakdownReport
        {
            Id = Guid.NewGuid(),
            RoverId = request.RoverId,
            ReportedById = userId,
            SymptomCategory = request.SymptomCategory.Trim(),
            Description = request.Description.Trim(),
            ErrorCode = string.IsNullOrWhiteSpace(request.ErrorCode) ? null : request.ErrorCode.Trim(),
            PhotoUrl = photoUrl,
            Status = BreakdownStatus.Reported,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _breakdownReportRepository.AddAsync(report, cancellationToken);
        await _breakdownReportRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Breakdown report {ReportId} created by user {UserId}", report.Id, userId);

        var detailedReport = await _breakdownReportRepository.GetByIdWithDetailsAsync(report.Id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = report.Id }, MapToResponseDto(detailedReport ?? report));
    }

    /// <summary>
    /// Retrieve paginated breakdown reports with optional status and category filters.
    /// Operators see only their own reports; Technicians and Supervisors see all.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedBreakdownReportsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReports(
        [FromQuery] BreakdownStatus? status = null,
        [FromQuery] string? symptomCategory = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var role = GetCurrentUserRole();
        var userId = GetCurrentUserId();

        Guid? filterUserId = null;
        // Operators can only view their own reports
        if (role == Role.Operator)
        {
            filterUserId = userId;
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount) = await _breakdownReportRepository.GetPagedAsync(
            status,
            symptomCategory,
            filterUserId,
            page,
            pageSize,
            cancellationToken);

        var response = new PagedBreakdownReportsDto
        {
            Items = items.Select(MapToResponseDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Retrieve a single breakdown report by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BreakdownReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var report = await _breakdownReportRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (report == null)
        {
            return NotFound(new { message = $"Breakdown report '{id}' was not found." });
        }

        var role = GetCurrentUserRole();
        var userId = GetCurrentUserId();

        // Operators can only view their own report
        if (role == Role.Operator && report.ReportedById != userId)
        {
            return Forbid();
        }

        return Ok(MapToResponseDto(report));
    }

    /// <summary>
    /// Update the lifecycle status of a breakdown report (Technicians and Supervisors only).
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Technician,Supervisor")]
    [ProducesResponseType(typeof(BreakdownReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateBreakdownStatusDto request, CancellationToken cancellationToken)
    {
        var report = await _breakdownReportRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (report == null)
        {
            return NotFound(new { message = $"Breakdown report '{id}' was not found." });
        }

        report.Status = request.Status;
        report.UpdatedAt = DateTime.UtcNow;

        await _breakdownReportRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Breakdown report {ReportId} status updated to {Status}", report.Id, report.Status);

        return Ok(MapToResponseDto(report));
    }

    /// <summary>
    /// Trigger the autonomous Maintenance Mechanic Agent to diagnose a breakdown report.
    /// </summary>
    [HttpPost("{id:guid}/diagnose")]
    [ProducesResponseType(typeof(MaintenanceMechanicOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Diagnose(Guid id, CancellationToken cancellationToken)
    {
        var report = await _breakdownReportRepository.GetByIdAsync(id, cancellationToken);
        if (report == null)
        {
            return NotFound(new { message = $"Breakdown report '{id}' was not found." });
        }

        var agentInput = new MaintenanceMechanicInput
        {
            BreakdownReportId = report.Id.ToString(),
            SymptomCategory = report.SymptomCategory,
            Description = report.Description,
            ErrorCode = report.ErrorCode
        };

        var diagnosis = await _mechanicAgent.DiagnoseAsync(agentInput, cancellationToken);

        // Store diagnosis JSON on the breakdown report
        report.DiagnosisResultJson = JsonSerializer.Serialize(diagnosis);
        if (report.Status == BreakdownStatus.Reported)
        {
            report.Status = diagnosis.RecommendedAction == "ScheduleRepair"
                ? BreakdownStatus.ScheduledForRepair
                : BreakdownStatus.Diagnosing;
        }
        report.UpdatedAt = DateTime.UtcNow;

        await _breakdownReportRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Breakdown report {ReportId} diagnosed: {LikelyPart}, action: {Action}",
            report.Id, diagnosis.LikelyPart, diagnosis.RecommendedAction);

        return Ok(diagnosis);
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("nameid")?.Value
            ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(claim, out var guid) ? guid : Guid.Empty;
    }

    private Role GetCurrentUserRole()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value
            ?? User.FindFirst("role")?.Value;

        return Enum.TryParse<Role>(roleClaim, true, out var role) ? role : Role.Operator;
    }

    private static BreakdownReportResponseDto MapToResponseDto(BreakdownReport report)
    {
        MaintenanceMechanicOutput? diagnosis = null;
        if (!string.IsNullOrWhiteSpace(report.DiagnosisResultJson))
        {
            try
            {
                diagnosis = JsonSerializer.Deserialize<MaintenanceMechanicOutput>(report.DiagnosisResultJson);
            }
            catch
            {
                // Fallback if parsing fails
            }
        }

        return new BreakdownReportResponseDto
        {
            Id = report.Id,
            RoverId = report.RoverId,
            ReportedById = report.ReportedById,
            ReportedByName = report.ReportedBy?.Name ?? "SmartFleet Operator",
            ReportedByEmail = report.ReportedBy?.Email ?? string.Empty,
            SymptomCategory = report.SymptomCategory,
            Description = report.Description,
            PhotoUrl = report.PhotoUrl,
            ErrorCode = report.ErrorCode,
            Status = report.Status,
            DiagnosisResult = diagnosis,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt
        };
    }
}
