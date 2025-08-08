using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Temple.Application.Automation;
using Temple.Infrastructure.Persistence;

namespace Temple.Infrastructure.Temple.Automation;

public class LessonRotationJob : ILessonRotationJob
{
    private readonly AppDbContext _db;
    private readonly ILogger<LessonRotationJob> _logger;
    public LessonRotationJob(AppDbContext db, ILogger<LessonRotationJob> logger)
    { _db = db; _logger = logger; }

    public async Task RunAsync(CancellationToken ct = default)
    {
        // Rotate per-tenant if no manual override
        var tenantStates = await _db.LessonAutomationStates.AsTracking().ToListAsync(ct);
        foreach (var state in tenantStates)
        {
            if (state.ManualOverride)
            {
                _logger.LogDebug("Skipping rotation for tenant {TenantId} due to manual override", state.TenantId);
                continue;
            }
            // Pick next published lesson least recently featured
            var lesson = await _db.Lessons
                .Where(l => l.TenantId == state.TenantId && l.PublishedUtc != null)
                .OrderBy(l => l.LastFeaturedUtc ?? DateTime.MinValue)
                .ThenBy(l => l.PublishedUtc)
                .FirstOrDefaultAsync(ct);
            if (lesson == null)
            {
                _logger.LogDebug("No published lessons for tenant {TenantId} to rotate", state.TenantId);
                continue;
            }
            state.ActiveLessonId = lesson.Id;
            state.LastRotationUtc = DateTime.UtcNow;
            lesson.LastFeaturedUtc = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }
}
