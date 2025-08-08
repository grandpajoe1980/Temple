namespace Temple.Domain.Stewardship;

public class NonCashGift
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? DonorPersonId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal? EstimatedValue { get; set; }
    public string? AppraisalDocumentUrl { get; set; }
    public DateTime ReceivedUtc { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
