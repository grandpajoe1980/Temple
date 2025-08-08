using Microsoft.EntityFrameworkCore;
using Temple.Application.Search;
using Temple.Infrastructure.Persistence;

namespace Temple.Infrastructure.Temple.Search;

public class NaiveSearchService : ISearchService
{
    private readonly AppDbContext _db;
    public NaiveSearchService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<object>> SearchAsync(Guid tenantId, string query, int limit, CancellationToken ct)
    {
        limit = limit is <=0 or >50 ? 20 : limit;
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<object>();
        var schedule = await _db.ScheduleEvents
            .Where(e => e.TenantId == tenantId && (EF.Functions.ILike(e.Title, $"%{query}%") || (e.Description != null && EF.Functions.ILike(e.Description, $"%{query}%"))))
            .Select(e => new { type = "event", e.Id, e.Title, e.StartUtc })
            .Take(limit)
            .ToListAsync(ct);
        var messages = await _db.ChatMessages
            .Where(m => m.TenantId == tenantId && EF.Functions.ILike(m.Body, $"%{query}%"))
            .OrderByDescending(m => m.CreatedUtc)
            .Select(m => new { type = "chat", m.Id, m.Body, m.CreatedUtc })
            .Take(limit)
            .ToListAsync(ct);
        var lessons = await _db.Lessons
            .Where(l => l.TenantId == tenantId && (EF.Functions.ILike(l.Title, $"%{query}%") || EF.Functions.ILike(l.Body, $"%{query}%")))
            .OrderByDescending(l => l.PublishedUtc ?? l.CreatedUtc)
            .Select(l => new { type = "lesson", l.Id, l.Title, l.PublishedUtc })
            .Take(limit)
            .ToListAsync(ct);
        return schedule.Cast<object>().Concat(messages).Concat(lessons).Take(limit).ToList();
    }
}
