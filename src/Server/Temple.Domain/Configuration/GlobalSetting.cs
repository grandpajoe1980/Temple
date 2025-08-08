namespace Temple.Domain.Configuration;

// Represents a single global (platform-wide) configuration value, not tenant-scoped.
public class GlobalSetting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = string.Empty; // e.g., smtp.host, feature.flag.x
    public string Value { get; set; } = string.Empty; // raw or JSON serialized value
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedByUserId { get; set; }
}
