namespace Temple.Domain.People;

public class HouseholdMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid HouseholdId { get; set; }
    public Guid PersonId { get; set; }
    public string Relationship { get; set; } = "member"; // head, spouse, child, member
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
