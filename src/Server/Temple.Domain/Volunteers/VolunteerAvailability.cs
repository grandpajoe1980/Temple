namespace Temple.Domain.Volunteers;

public class VolunteerAvailability
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PersonId { get; set; }
    public string Pattern { get; set; } = string.Empty; // e.g. CRON-like or simple JSON spec
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
}
