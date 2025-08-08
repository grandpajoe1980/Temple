namespace Temple.Domain.Volunteers;

public class VolunteerAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PositionId { get; set; }
    public Guid PersonId { get; set; }
    public DateTime StartUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndUtc { get; set; }
    public string? Notes { get; set; }
}
