namespace Temple.Domain.Volunteers;

public class VolunteerBackgroundCheck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PersonId { get; set; }
    public string Status { get; set; } = "pending"; // pending, clear, flagged, expired
    public DateTime RequestedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedUtc { get; set; }
    public DateTime? ExpiresUtc { get; set; }
    public string? Reference { get; set; }
}
