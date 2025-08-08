using Hangfire;
using Microsoft.EntityFrameworkCore;
using Temple.Application.Scheduling;
using Temple.Domain.Scheduling;
using Temple.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace Temple.Infrastructure.Temple.Scheduling;

public class EventReminderScheduler : IEventReminderScheduler
{
    private readonly AppDbContext _db;
    private readonly IBackgroundJobClient _background;
    private readonly ILogger<EventReminderScheduler> _logger;
    public EventReminderScheduler(AppDbContext db, IBackgroundJobClient background, ILogger<EventReminderScheduler> logger)
    { _db = db; _background = background; _logger = logger; }

    public async Task ScheduleAsync(Guid tenantId, Guid eventId, DateTime eventStartUtc, IEnumerable<int> minutesBefore, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        foreach (var m in minutesBefore.Distinct().Where(v => v > 0 && v <= 60 * 24 * 30))
        {
            var scheduleTime = eventStartUtc.AddMinutes(-m);
            if (scheduleTime <= now) continue; // skip past times
            var jobId = _background.Schedule(() => ExecuteReminder(eventId, tenantId, m), scheduleTime - now);
            _db.EventReminders.Add(new EventReminder { TenantId = tenantId, EventId = eventId, MinutesBefore = m, ScheduledUtc = scheduleTime, JobId = jobId });
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task RescheduleAsync(Guid tenantId, Guid eventId, DateTime newStartUtc, IEnumerable<int>? minutesBefore = null, CancellationToken ct = default)
    {
        var existing = await _db.EventReminders.Where(r => r.TenantId == tenantId && r.EventId == eventId).ToListAsync(ct);
        foreach (var r in existing)
        {
            if (!string.IsNullOrWhiteSpace(r.JobId)) BackgroundJob.Delete(r.JobId);
        }
        _db.EventReminders.RemoveRange(existing);
        await _db.SaveChangesAsync(ct);
        await ScheduleAsync(tenantId, eventId, newStartUtc, minutesBefore ?? existing.Select(e => e.MinutesBefore), ct);
    }

    public async Task CancelAsync(Guid tenantId, Guid eventId, CancellationToken ct = default)
    {
        var existing = await _db.EventReminders.Where(r => r.TenantId == tenantId && r.EventId == eventId).ToListAsync(ct);
        foreach (var r in existing)
        {
            if (!string.IsNullOrWhiteSpace(r.JobId)) BackgroundJob.Delete(r.JobId);
        }
        _db.EventReminders.RemoveRange(existing);
        await _db.SaveChangesAsync(ct);
    }

    // Executed by Hangfire
    public Task ExecuteReminder(Guid eventId, Guid tenantId, int minutesBefore)
    {
        _logger.LogInformation("Reminder: Event {EventId} for tenant {TenantId} starting in {Minutes} minutes", eventId, tenantId, minutesBefore);
        // Future: enqueue notification dispatch
        return Task.CompletedTask;
    }
}
