namespace Temple.Domain.Stewardship;

public class StewardshipFund
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; } // optional short code
    public string? Description { get; set; }
    public decimal Balance { get; set; } // denormalized running balance
    public bool IsArchived { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
