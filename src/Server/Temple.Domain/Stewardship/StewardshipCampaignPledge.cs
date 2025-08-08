namespace Temple.Domain.Stewardship;

public class StewardshipCampaignPledge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid PersonId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FulfilledUtc { get; set; }
}
