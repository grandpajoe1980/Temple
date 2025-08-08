namespace Temple.Domain.Content;

public class DailyContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TaxonomyId { get; set; } = string.Empty; // religion or sect scope
    public string Type { get; set; } = "daily_thought";
    public string Body { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
