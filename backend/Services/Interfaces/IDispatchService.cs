using SmartFleet.Backend.Agents.MissionPlannerAgent;
using SmartFleet.Backend.DTOs.Common;
using SmartFleet.Backend.DTOs.Dispatch;
using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.Services.Interfaces;

public interface IDispatchService
{
    Task<DispatchRequestResponseDto> CreateDispatchRequestAsync(
        CreateDispatchRequestDto dto,
        Guid operatorId,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<DispatchRequestResponseDto>> GetDispatchRequestsAsync(
        DispatchRequestQueryDto query,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken cancellationToken = default);

    Task<DispatchRequestResponseDto> GetDispatchRequestByIdAsync(
        Guid id,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken cancellationToken = default);

    Task<DispatchRequestResponseDto> UpdateStatusAsync(
        Guid id,
        DispatchRequestStatus status,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken cancellationToken = default);

    Task<MissionPlannerOutput> GeneratePlanAsync(
        Guid id,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken cancellationToken = default);
}
