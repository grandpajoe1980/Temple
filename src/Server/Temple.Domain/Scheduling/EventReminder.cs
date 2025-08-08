namespace Temple.Domain.Scheduling;

public class EventReminder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public int MinutesBefore { get; set; }
    public DateTime ScheduledUtc { get; set; }
    public string? JobId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
