namespace Temple.Domain.Chat;

public class ChatPresence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
    public DateTime ConnectedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DisconnectedUtc { get; set; }
    public DateTime LastActiveUtc { get; set; } = DateTime.UtcNow;
}
