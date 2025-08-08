namespace Temple.Domain.Finance;

public class Expense
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? SubmittedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public Guid BudgetCategoryId { get; set; }
    public long AmountCents { get; set; }
    public string Currency { get; set; } = "usd";
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "submitted"; // submitted, approved, rejected, paid
    public DateTime SubmittedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedUtc { get; set; }
    public DateTime? PaidUtc { get; set; }
    public string? Notes { get; set; }
}
