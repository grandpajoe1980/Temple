namespace Temple.Application.Scheduling;

public interface IEventReminderScheduler
{
    Task ScheduleAsync(Guid tenantId, Guid eventId, DateTime eventStartUtc, IEnumerable<int> minutesBefore, CancellationToken ct = default);
    Task RescheduleAsync(Guid tenantId, Guid eventId, DateTime newStartUtc, IEnumerable<int>? minutesBefore = null, CancellationToken ct = default);
    Task CancelAsync(Guid tenantId, Guid eventId, CancellationToken ct = default);
}
