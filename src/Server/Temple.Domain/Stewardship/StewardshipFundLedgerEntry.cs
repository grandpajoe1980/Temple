namespace Temple.Domain.Stewardship;

public class StewardshipFundLedgerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid FundId { get; set; }
    public Guid? CampaignId { get; set; }
    public Guid? DonationId { get; set; }
    public decimal Amount { get; set; } // positive credit, negative debit
    public string? Type { get; set; } // e.g., donation, adjustment, transfer
    public string? Notes { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
