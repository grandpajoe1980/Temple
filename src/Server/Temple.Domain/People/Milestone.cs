namespace Temple.Domain.People;

public class Milestone
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PersonId { get; set; }
    public string Type { get; set; } = string.Empty; // baptism, membership, dedication, etc.
    public DateTime DateUtc { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
