using Microsoft.EntityFrameworkCore;
using SmartFleet.Backend.Models;

namespace SmartFleet.Backend.Data.Repositories;

public class FailureCatalogRepository : IFailureCatalogRepository
{
    private readonly SmartFleetDbContext _context;

    public FailureCatalogRepository(SmartFleetDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<FailureCatalog>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FailureCatalogs
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<FailureCatalog?> FindMatchAsync(string symptomCategory, string description, string? errorCode, CancellationToken cancellationToken = default)
    {
        var catalog = await _context.FailureCatalogs
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var descLower = (description ?? string.Empty).ToLowerInvariant();
        var catNormalized = (symptomCategory ?? string.Empty).ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "");

        // 1. Try keyword matching in description
        var match = catalog.FirstOrDefault(c => descLower.Contains(c.SymptomKeyword.ToLowerInvariant()));
        if (match != null) return match;

        // 2. Try matching symptom category (ignoring casing & spacing)
        match = catalog.FirstOrDefault(c =>
            c.SymptomCategory.ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "") == catNormalized ||
            catNormalized.Contains(c.SymptomKeyword.ToLowerInvariant().Replace(" ", "")));
        if (match != null) return match;

        // 3. Try partial keyword matching against words in description
        var words = descLower.Split(new[] { ' ', ',', '.', ';', '!' }, StringSplitOptions.RemoveEmptyEntries);
        match = catalog.FirstOrDefault(c => words.Any(w => w.Length > 4 && c.SymptomKeyword.ToLowerInvariant().Contains(w)));
        if (match != null) return match;

        return null;
    }
}
