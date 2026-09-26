using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SmartFleet.Backend.Agents.MaintenanceMechanicAgent;
using SmartFleet.Backend.Agents.MaintenanceMechanicAgent.DTOs;
using SmartFleet.Backend.Data.Repositories;
using SmartFleet.Backend.Models;
using Xunit;

namespace SmartFleet.Backend.Tests.Agents;

public class MaintenanceMechanicAgentTests
{
    private class FakeFailureCatalogRepository : IFailureCatalogRepository
    {
        private readonly List<FailureCatalog> _items;

        public FakeFailureCatalogRepository(IEnumerable<FailureCatalog>? seed = null)
        {
            _items = seed != null ? new List<FailureCatalog>(seed) : new List<FailureCatalog>();
        }

        public Task<IReadOnlyList<FailureCatalog>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<FailureCatalog>>(_items);
        }

        public Task<FailureCatalog?> FindMatchAsync(string symptomCategory, string description, string? errorCode, CancellationToken cancellationToken = default)
        {
            var descLower = (description ?? string.Empty).ToLowerInvariant();
            var catNormalized = (symptomCategory ?? string.Empty).ToLowerInvariant().Replace(" ", "");

            var match = _items.FirstOrDefault(c => descLower.Contains(c.SymptomKeyword.ToLowerInvariant()));
            if (match != null) return Task.FromResult<FailureCatalog?>(match);

            match = _items.FirstOrDefault(c => c.SymptomCategory.ToLowerInvariant().Replace(" ", "") == catNormalized ||
                                               catNormalized.Contains(c.SymptomKeyword.ToLowerInvariant().Replace(" ", "")));
            if (match != null) return Task.FromResult<FailureCatalog?>(match);

            return Task.FromResult<FailureCatalog?>(null);
        }
    }

    [Fact]
    public async Task DiagnoseAsync_WhenCatalogMatches_ShouldReturnExactContractSampleValues()
    {
        // Arrange - Setup catalog matching docs/agent-contracts.md sample
        var matchedCatalogEntry = new FailureCatalog
        {
            Id = Guid.NewGuid(),
            SymptomKeyword = "motor overheating",
            SymptomCategory = "MotorOverheating",
            LikelyPart = "Drive Motor Unit",
            EstimatedRepairHours = 2,
            Severity = "High"
        };

        var repo = new FakeFailureCatalogRepository(new[] { matchedCatalogEntry });
        var agent = new MaintenanceMechanicAgent(repo, NullLogger<MaintenanceMechanicAgent>.Instance);

        var input = new MaintenanceMechanicInput
        {
            BreakdownReportId = "b7c2-441e",
            SymptomCategory = "MotorOverheating",
            Description = "Front wheel motor making grinding noise",
            ErrorCode = "E204"
        };

        // Act
        var result = await agent.DiagnoseAsync(input);

        // Assert - strictly asserting contract fields
        Assert.NotNull(result);
        Assert.Equal("b7c2-441e", result.BreakdownReportId);
        Assert.Equal("Drive Motor Unit", result.LikelyPart);
        Assert.Equal(2, result.EstimatedRepairHours);
        Assert.Equal("High", result.Severity);
        Assert.Contains("motor overheating", result.ConfidenceNote);
        Assert.Equal("ScheduleRepair", result.RecommendedAction);
    }

    [Fact]
    public async Task DiagnoseAsync_WhenNoCatalogMatches_ShouldReturnNeedsManualReview()
    {
        // Arrange - Empty catalog yields no match
        var repo = new FakeFailureCatalogRepository();
        var agent = new MaintenanceMechanicAgent(repo, NullLogger<MaintenanceMechanicAgent>.Instance);

        var input = new MaintenanceMechanicInput
        {
            BreakdownReportId = "test-unmatched-999",
            SymptomCategory = "AestheticDent",
            Description = "Front sticker slightly peeled after shelf collision",
            ErrorCode = null
        };

        // Act
        var result = await agent.DiagnoseAsync(input);

        // Assert - contract requirement for unmatched cases
        Assert.NotNull(result);
        Assert.Equal("test-unmatched-999", result.BreakdownReportId);
        Assert.Equal("NeedsManualReview", result.RecommendedAction);
        Assert.Equal("Unknown / Requires Inspection", result.LikelyPart);
        Assert.Equal(0, result.EstimatedRepairHours);
    }

    [Fact]
    public void ContractSerialization_MustMatchExactCasingInAgentContractsDocument()
    {
        // Arrange
        var output = new MaintenanceMechanicOutput
        {
            BreakdownReportId = "b7c2-441e",
            LikelyPart = "Drive Motor Unit",
            EstimatedRepairHours = 2,
            Severity = "High",
            ConfidenceNote = "Matched via FailureCatalog keyword: motor overheating",
            RecommendedAction = "ScheduleRepair"
        };

        // Act
        var json = JsonSerializer.Serialize(output);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert that the exact property names from docs/agent-contracts.md exist
        Assert.True(root.TryGetProperty("breakdownReportId", out var reportIdProp));
        Assert.Equal("b7c2-441e", reportIdProp.GetString());

        Assert.True(root.TryGetProperty("likelyPart", out var likelyPartProp));
        Assert.Equal("Drive Motor Unit", likelyPartProp.GetString());

        Assert.True(root.TryGetProperty("estimatedRepairHours", out var repairHoursProp));
        Assert.Equal(2, repairHoursProp.GetInt32());

        Assert.True(root.TryGetProperty("severity", out var severityProp));
        Assert.Equal("High", severityProp.GetString());

        Assert.True(root.TryGetProperty("confidenceNote", out var noteProp));
        Assert.Contains("motor overheating", noteProp.GetString());

        Assert.True(root.TryGetProperty("recommendedAction", out var actionProp));
        Assert.Equal("ScheduleRepair", actionProp.GetString());

        // Assert no alternative casings or incorrect field names exist
        Assert.False(root.TryGetProperty("LikelyPart", out _));
        Assert.False(root.TryGetProperty("likely_part", out _));
        Assert.False(root.TryGetProperty("recommended_action", out _));
        Assert.False(root.TryGetProperty("RecommendedAction", out _));
    }
}
