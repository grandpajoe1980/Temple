namespace Temple.Domain.Stewardship;

public class StewardshipCampaign
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal GoalAmount { get; set; }
    public decimal RaisedAmount { get; set; } // denormalized quick access
    public DateTime StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
