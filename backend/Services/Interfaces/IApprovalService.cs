using SmartFleet.Backend.DTOs.Approvals;

namespace SmartFleet.Backend.Services.Interfaces;

public interface IApprovalService
{
    Task<PagedApprovalResultDto> GetPagedApprovalsAsync(string? status, string? sortBy, string? sortOrder, string? search, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);

    Task<ApprovalRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ApprovalRequestDto> ApproveAsync(Guid id, Guid? supervisorId, string? notes, CancellationToken cancellationToken = default);

    Task<ApprovalRequestDto> RejectAsync(Guid id, Guid? supervisorId, string? notes, CancellationToken cancellationToken = default);

    Task<ApprovalRequestDto> RequestRevisionAsync(Guid id, Guid? supervisorId, string? notes, CancellationToken cancellationToken = default);

    Task<List<WorkflowExecutionLogDto>> GetExecutionLogsAsync(Guid approvalRequestId, CancellationToken cancellationToken = default);

    Task<ApprovalStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);

    Task SeedDemoDataIfEmptyAsync(CancellationToken cancellationToken = default);
}
