namespace Temple.Domain.Content;

public class MediaAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = "audio"; // audio, video, document
    public string StorageKey { get; set; } = string.Empty; // path or external reference
    public string Status { get; set; } = "pending"; // pending, uploading, processing, ready, failed
    public int? DurationSeconds { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
}
