using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Temple.Application.Tenants;
using Temple.Infrastructure.Persistence;

namespace Temple.Tests;

public class TenantTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TenantTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");
        _factory = factory;
    }

    [Fact]
    public async Task Can_Create_Tenant_With_Name_Only()
    {
        var client = _factory.CreateClient();
        var request = new TenantCreateRequest("Test Tenant", null, null);
        
        var response = await client.PostAsJsonAsync("/api/v1/tenants", request);
        
        response.EnsureSuccessStatusCode();
        var tenant = await response.Content.ReadFromJsonAsync<TenantResponse>();
        
        Assert.NotNull(tenant);
        Assert.Equal("Test Tenant", tenant.Name);
        Assert.Equal("test-tenant", tenant.Slug);
        Assert.NotEqual(Guid.Empty, tenant.Id);
    }

    [Fact]
    public async Task Can_Create_Tenant_With_Religion()
    {
        var client = _factory.CreateClient();
        
        // First, we need to seed a religion in the database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var religion = new Temple.Domain.Taxonomy.ReligionTaxonomy
        {
            Id = "test-religion-1",
            DisplayName = "Test Religion",
            Type = "religion",
            ParentId = null
        };
        db.ReligionTaxonomies.Add(religion);
        await db.SaveChangesAsync();
        
        var request = new TenantCreateRequest("Test Church", null, "test-religion-1");
        var response = await client.PostAsJsonAsync("/api/v1/tenants", request);
        
        response.EnsureSuccessStatusCode();
        var tenant = await response.Content.ReadFromJsonAsync<TenantResponse>();
        
        Assert.NotNull(tenant);
        Assert.Equal("Test Church", tenant.Name);
        Assert.Equal("test-church", tenant.Slug);
        Assert.Equal("test-religion-1", tenant.TaxonomyId);
    }

    [Fact]
    public async Task Can_Create_Tenant_With_Sect()
    {
        var client = _factory.CreateClient();
        
        // Seed religion and sect
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var religion = new Temple.Domain.Taxonomy.ReligionTaxonomy
        {
            Id = "test-religion-2",
            DisplayName = "Test Religion 2",
            Type = "religion",
            ParentId = null
        };
        db.ReligionTaxonomies.Add(religion);
        
        var sect = new Temple.Domain.Taxonomy.ReligionTaxonomy
        {
            Id = "test-sect-1",
            DisplayName = "Test Sect",
            Type = "sect",
            ParentId = "test-religion-2"
        };
        db.ReligionTaxonomies.Add(sect);
        await db.SaveChangesAsync();
        
        var request = new TenantCreateRequest("Test Congregation", "test-sect-1", null);
        var response = await client.PostAsJsonAsync("/api/v1/tenants", request);
        
        response.EnsureSuccessStatusCode();
        var tenant = await response.Content.ReadFromJsonAsync<TenantResponse>();
        
        Assert.NotNull(tenant);
        Assert.Equal("Test Congregation", tenant.Name);
        Assert.Equal("test-congregation", tenant.Slug);
        Assert.Equal("test-sect-1", tenant.TaxonomyId);
    }

    [Fact]
    public async Task Can_Get_Tenant_By_Id()
    {
        var client = _factory.CreateClient();
        
        // Create a tenant first
        var createRequest = new TenantCreateRequest("Tenant To Get", null, null);
        var createResponse = await client.PostAsJsonAsync("/api/v1/tenants", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdTenant = await createResponse.Content.ReadFromJsonAsync<TenantResponse>();
        
        Assert.NotNull(createdTenant);
        
        // Get the tenant by ID
        var getResponse = await client.GetAsync($"/api/v1/tenants/{createdTenant.Id}");
        getResponse.EnsureSuccessStatusCode();
        var tenant = await getResponse.Content.ReadFromJsonAsync<TenantResponse>();
        
        Assert.NotNull(tenant);
        Assert.Equal(createdTenant.Id, tenant.Id);
        Assert.Equal("Tenant To Get", tenant.Name);
        Assert.Equal("tenant-to-get", tenant.Slug);
    }

    [Fact]
    public async Task Can_Get_Tenant_By_Slug()
    {
        var client = _factory.CreateClient();
        
        // Create a tenant first
        var createRequest = new TenantCreateRequest("Tenant By Slug", null, null);
        var createResponse = await client.PostAsJsonAsync("/api/v1/tenants", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdTenant = await createResponse.Content.ReadFromJsonAsync<TenantResponse>();
        
        Assert.NotNull(createdTenant);
        
        // Get the tenant by slug
        var getResponse = await client.GetAsync($"/api/v1/tenants/by-slug/{createdTenant.Slug}");
        getResponse.EnsureSuccessStatusCode();
        var tenant = await getResponse.Content.ReadFromJsonAsync<TenantResponse>();
        
        Assert.NotNull(tenant);
        Assert.Equal(createdTenant.Id, tenant.Id);
        Assert.Equal("Tenant By Slug", tenant.Name);
        Assert.Equal("tenant-by-slug", tenant.Slug);
    }

    [Fact]
    public async Task Duplicate_Name_Creates_Unique_Slug()
    {
        var client = _factory.CreateClient();
        
        // Create first tenant
        var request1 = new TenantCreateRequest("Same Name", null, null);
        var response1 = await client.PostAsJsonAsync("/api/v1/tenants", request1);
        response1.EnsureSuccessStatusCode();
        var tenant1 = await response1.Content.ReadFromJsonAsync<TenantResponse>();
        
        // Create second tenant with same name
        var request2 = new TenantCreateRequest("Same Name", null, null);
        var response2 = await client.PostAsJsonAsync("/api/v1/tenants", request2);
        response2.EnsureSuccessStatusCode();
        var tenant2 = await response2.Content.ReadFromJsonAsync<TenantResponse>();
        
        Assert.NotNull(tenant1);
        Assert.NotNull(tenant2);
        Assert.Equal("same-name", tenant1.Slug);
        Assert.Equal("same-name-1", tenant2.Slug);
        Assert.NotEqual(tenant1.Id, tenant2.Id);
    }

    [Fact]
    public async Task Can_List_All_Tenants()
    {
        var client = _factory.CreateClient();
        
        // Create a couple of tenants
        await client.PostAsJsonAsync("/api/v1/tenants", new TenantCreateRequest("List Test 1", null, null));
        await client.PostAsJsonAsync("/api/v1/tenants", new TenantCreateRequest("List Test 2", null, null));
        
        // List all tenants
        var response = await client.GetAsync("/api/v1/tenants");
        response.EnsureSuccessStatusCode();
        var tenants = await response.Content.ReadFromJsonAsync<List<TenantResponse>>();
        
        Assert.NotNull(tenants);
        Assert.True(tenants.Count >= 2);
        Assert.Contains(tenants, t => t.Name == "List Test 1");
        Assert.Contains(tenants, t => t.Name == "List Test 2");
    }

    [Fact]
    public async Task Invalid_Tenant_Name_Returns_Error()
    {
        var client = _factory.CreateClient();
        var request = new TenantCreateRequest("", null, null);
        
        var response = await client.PostAsJsonAsync("/api/v1/tenants", request);
        
        Assert.False(response.IsSuccessStatusCode);
    }

    private record TenantResponse(Guid Id, string Name, string Slug, string? Status, string? TaxonomyId);
}
