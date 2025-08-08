namespace Temple.Domain.Scheduling;

public class ScheduleEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string Type { get; set; } = "service"; // service, study, lecture
    public string? Description { get; set; }
    public string? Category { get; set; } // optional category label (e.g., worship, study, outreach)
    public string? RecurrenceRule { get; set; } // RFC5545 RRULE (null for one-off)
    public DateTime? RecurrenceEndUtc { get; set; } // optional until date for recurrence
    public Guid? SeriesId { get; set; } // groups generated instances
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
