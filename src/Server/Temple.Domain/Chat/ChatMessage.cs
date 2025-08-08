namespace Temple.Domain.Chat;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ChannelId { get; set; }
    public Guid UserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? Type { get; set; } // standard, announcement
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
