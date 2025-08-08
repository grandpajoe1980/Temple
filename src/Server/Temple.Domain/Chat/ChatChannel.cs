namespace Temple.Domain.Chat;

public class ChatChannel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Key { get; set; } = string.Empty; // e.g., general, announcements
    public string Name { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public bool IsPrivate { get; set; }
    public string? Description { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
