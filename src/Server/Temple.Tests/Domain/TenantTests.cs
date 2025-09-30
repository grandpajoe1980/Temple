using Temple.Domain.Tenants;

namespace Temple.Tests.Domain;

public class TenantTests
{
    [Fact]
    public void New_Tenant_Has_Unique_Id()
    {
        var tenant1 = new Tenant();
        var tenant2 = new Tenant();
        
        Assert.NotEqual(tenant1.Id, tenant2.Id);
    }

    [Fact]
    public void New_Tenant_Has_Default_Active_Status()
    {
        var tenant = new Tenant();
        
        Assert.Equal("active", tenant.Status);
    }

    [Fact]
    public void New_Tenant_Has_Creation_Timestamp()
    {
        var before = DateTime.UtcNow;
        var tenant = new Tenant();
        var after = DateTime.UtcNow;
        
        Assert.InRange(tenant.CreatedUtc, before.AddSeconds(-1), after.AddSeconds(1));
    }

    [Fact]
    public void Tenant_Can_Be_Created_With_Properties()
    {
        var tenant = new Tenant
        {
            Name = "Test Tenant",
            Slug = "test-tenant",
            Status = "active",
            TaxonomyId = "sect-123"
        };
        
        Assert.Equal("Test Tenant", tenant.Name);
        Assert.Equal("test-tenant", tenant.Slug);
        Assert.Equal("active", tenant.Status);
        Assert.Equal("sect-123", tenant.TaxonomyId);
    }

    [Theory]
    [InlineData("active")]
    [InlineData("suspended")]
    [InlineData("archived")]
    public void Tenant_Can_Have_Different_Statuses(string status)
    {
        var tenant = new Tenant { Status = status };
        
        Assert.Equal(status, tenant.Status);
    }
}
