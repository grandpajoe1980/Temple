namespace Temple.Domain.Chat;

public class ChatChannelMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ChannelId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedUtc { get; set; } = DateTime.UtcNow;
    public bool IsModerator { get; set; }
}
