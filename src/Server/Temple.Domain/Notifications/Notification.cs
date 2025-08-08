namespace Temple.Domain.Notifications;

public enum NotificationDeliveryStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2
}

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; } // null for broadcast (fan-out at read time)
    public string Channel { get; set; } = string.Empty; // email, push, inapp
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SentUtc { get; set; }
    public NotificationDeliveryStatus Status { get; set; } = NotificationDeliveryStatus.Pending;
    public int Attempts { get; set; }
    public string? Error { get; set; }
    // Direct notifications: read state inline; broadcast notifications store per-user state in NotificationUserState table
    public DateTime? ReadUtc { get; set; }
}

public class NotificationPreference
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Channel { get; set; } = string.Empty; // email, push, inapp
    public bool Enabled { get; set; } = true;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

// Read tracking for broadcast notifications (those with null UserId)
public class NotificationUserState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
    public DateTime? ReadUtc { get; set; }
}
