namespace Temple.Domain.Audit;

public class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? DataJson { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
