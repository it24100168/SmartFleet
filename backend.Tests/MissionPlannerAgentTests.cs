using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SmartFleet.Backend.Agents.MissionPlannerAgent;
using SmartFleet.Backend.Data.Repositories;
using SmartFleet.Backend.DTOs.Common;
using SmartFleet.Backend.DTOs.Dispatch;
using SmartFleet.Backend.Models;
using SmartFleet.Backend.Models.Enums;
using SmartFleet.Backend.Services;
using Xunit;

namespace SmartFleet.Backend.Tests;

public class MissionPlannerAgentTests
{
    private readonly MissionPlannerAgent _agent;

    public MissionPlannerAgentTests()
    {
        _agent = new MissionPlannerAgent(NullLogger<MissionPlannerAgent>.Instance);
    }

    [Fact]
    public async Task GeneratePlanAsync_ShouldProduceOutputMatchingContractPropertiesAndCasing()
    {
        // Sample input adhering strictly to docs/agent-contracts.md
        var input = new MissionPlannerInput
        {
            DispatchRequestId = "d3f1-892a",
            SourceZone = "WarehouseA-DockA1",
            DestinationZone = "WarehouseA-DockB3",
            CargoType = "Fragile",
            Priority = "High",
            PreferredTimeWindow = "2026-09-19T15:00:00Z"
        };

        var output = await _agent.GeneratePlanAsync(input);

        // Verify top-level contract properties
        Assert.NotNull(output);
        Assert.Equal("d3f1-892a", output.DispatchRequestId);
        Assert.NotEmpty(output.CreatedAt);
        Assert.NotNull(output.Plan);
        Assert.NotEmpty(output.Plan);

        // Check each step conforms to contract
        foreach (var step in output.Plan)
        {
            Assert.True(step.StepNumber > 0);
            Assert.False(string.IsNullOrWhiteSpace(step.StepName));
            // Exact casing required by agent-contracts.md
            Assert.Equal("Pending", step.Status);
            // Architectural requirement: labeled with assigned agent
            Assert.False(string.IsNullOrWhiteSpace(step.AssignedAgent));
        }

        // Verify JSON serialization contains exact field names per agent-contracts.md
        var json = JsonSerializer.Serialize(output);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("dispatchRequestId", out var dIdProp));
        Assert.Equal("d3f1-892a", dIdProp.GetString());

        Assert.True(root.TryGetProperty("createdAt", out _));
        Assert.True(root.TryGetProperty("plan", out var planProp));
        Assert.Equal(JsonValueKind.Array, planProp.ValueKind);

        var firstStep = planProp[0];
        Assert.True(firstStep.TryGetProperty("stepNumber", out _));
        Assert.True(firstStep.TryGetProperty("stepName", out _));
        Assert.True(firstStep.TryGetProperty("status", out var statusProp));
        Assert.Equal("Pending", statusProp.GetString());
        Assert.True(firstStep.TryGetProperty("assignedAgent", out _));
    }

    [Fact]
    public async Task GeneratePlanAsync_ShouldLabelStepsWithAppropriateAgents()
    {
        var input = new MissionPlannerInput
        {
            DispatchRequestId = "test-req-001",
            SourceZone = "Zone-A",
            DestinationZone = "Zone-B",
            CargoType = "Hazmat",
            Priority = "Critical",
            PreferredTimeWindow = "2026-09-19T16:00:00Z"
        };

        var output = await _agent.GeneratePlanAsync(input);

        var telemetrySteps = output.Plan.Where(s => s.AssignedAgent == "DispatchTelemetryAgent").ToList();
        var safetySteps = output.Plan.Where(s => s.AssignedAgent == "SafetyGuardAgent").ToList();

        Assert.NotEmpty(telemetrySteps);
        Assert.NotEmpty(safetySteps);
        Assert.Contains(telemetrySteps, s => s.StepName.Contains("Locate available rover"));
        Assert.Contains(telemetrySteps, s => s.StepName.Contains("battery"));
        Assert.Contains(safetySteps, s => s.StepName.Contains("approval"));
    }

    [Fact]
    public async Task DispatchService_GeneratePlanAsync_ShouldPersistWorkflowRunAndUpdateStatus()
    {
        var dispatchId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();

        var dispatchRequest = new DispatchRequest
        {
            Id = dispatchId,
            OperatorId = operatorId,
            SourceZone = "Dock-1",
            DestinationZone = "Dock-2",
            CargoType = "Standard",
            Priority = "Medium",
            PreferredTimeWindow = DateTime.UtcNow.AddHours(2),
            Status = DispatchRequestStatus.Pending
        };

        var mockDispatchRepo = new InMemoryDispatchRequestRepository(dispatchRequest);
        var mockWorkflowRepo = new InMemoryWorkflowRunRepository();
        var mockUserRepo = new InMemoryUserRepository();

        var service = new DispatchService(
            mockDispatchRepo,
            mockWorkflowRepo,
            _agent,
            mockUserRepo,
            NullLogger<DispatchService>.Instance);

        var planOutput = await service.GeneratePlanAsync(dispatchId, operatorId, "Operator");

        // Verify output contract
        Assert.NotNull(planOutput);
        Assert.Equal(dispatchId.ToString(), planOutput.DispatchRequestId);

        // Verify DispatchRequest status updated to Planned
        Assert.Equal(DispatchRequestStatus.Planned, dispatchRequest.Status);

        // Verify WorkflowRun was persisted
        var savedRun = await mockWorkflowRepo.GetLatestByDispatchRequestIdAsync(dispatchId);
        Assert.NotNull(savedRun);
        Assert.Equal(dispatchId, savedRun.DispatchRequestId);
        Assert.Equal(1, savedRun.CurrentStep);
        Assert.Equal("Generated", savedRun.Status);
        Assert.False(string.IsNullOrWhiteSpace(savedRun.PlanJson));
        Assert.False(string.IsNullOrWhiteSpace(savedRun.ObjectiveJson));
    }

    // In-memory test test doubles
    private class InMemoryDispatchRequestRepository : IDispatchRequestRepository
    {
        private readonly List<DispatchRequest> _items = new();

        public InMemoryDispatchRequestRepository(params DispatchRequest[] initial)
        {
            _items.AddRange(initial);
        }

        public Task<DispatchRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_items.FirstOrDefault(x => x.Id == id));
        }

        public Task<PagedResultDto<DispatchRequest>> GetPagedAsync(DispatchRequestQueryDto query, Guid? operatorIdFilter = null, CancellationToken cancellationToken = default)
        {
            var filtered = _items.AsEnumerable();
            if (operatorIdFilter.HasValue)
                filtered = filtered.Where(x => x.OperatorId == operatorIdFilter.Value);

            var list = filtered.ToList();
            return Task.FromResult(new PagedResultDto<DispatchRequest>
            {
                Items = list,
                TotalCount = list.Count,
                Page = 1,
                PageSize = 10
            });
        }

        public Task AddAsync(DispatchRequest request, CancellationToken cancellationToken = default)
        {
            _items.Add(request);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private class InMemoryWorkflowRunRepository : IWorkflowRunRepository
    {
        private readonly List<WorkflowRun> _runs = new();

        public Task<WorkflowRun?> GetLatestByDispatchRequestIdAsync(Guid dispatchRequestId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_runs.Where(r => r.DispatchRequestId == dispatchRequestId).OrderByDescending(r => r.CreatedAt).FirstOrDefault());
        }

        public Task AddAsync(WorkflowRun workflowRun, CancellationToken cancellationToken = default)
        {
            _runs.Add(workflowRun);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private class InMemoryUserRepository : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(new User { Id = id, Name = "Test Operator", Email = "op@smartfleet.com", Role = Role.Operator });
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AddAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
