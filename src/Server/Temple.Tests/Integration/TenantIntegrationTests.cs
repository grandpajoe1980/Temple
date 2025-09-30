using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Temple.Application.Tenants;

namespace Temple.Tests.Integration;

public class TenantIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TenantIntegrationTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");
        _factory = factory;
    }

    [Fact]
    public async Task Create_Tenant_Returns_201_Created()
    {
        var client = _factory.CreateClient();
        var request = new TenantCreateRequest("New Tenant", null, null);

        var response = await client.PostAsJsonAsync("/api/v1/tenants", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Create_Tenant_Returns_Tenant_With_Id()
    {
        var client = _factory.CreateClient();
        var request = new TenantCreateRequest("Test Tenant", null, null);

        var response = await client.PostAsJsonAsync("/api/v1/tenants", request);
        var tenant = await response.Content.ReadFromJsonAsync<TenantResponse>();

        Assert.NotNull(tenant);
        Assert.NotEqual(Guid.Empty, tenant.Id);
        Assert.Equal("Test Tenant", tenant.Name);
    }

    [Fact]
    public async Task Create_Tenant_Generates_Slug_From_Name()
    {
        var client = _factory.CreateClient();
        var request = new TenantCreateRequest("My Test Organization", null, null);

        var response = await client.PostAsJsonAsync("/api/v1/tenants", request);
        var tenant = await response.Content.ReadFromJsonAsync<TenantResponse>();

        Assert.NotNull(tenant);
        Assert.Equal("my-test-organization", tenant.Slug);
    }

    [Fact]
    public async Task Get_Tenant_By_Id_Returns_200_Ok()
    {
        var client = _factory.CreateClient();
        var createRequest = new TenantCreateRequest("Get Test Tenant", null, null);
        var createResponse = await client.PostAsJsonAsync("/api/v1/tenants", createRequest);
        var createdTenant = await createResponse.Content.ReadFromJsonAsync<TenantResponse>();

        var getResponse = await client.GetAsync($"/api/v1/tenants/{createdTenant!.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task Get_Tenant_By_Slug_Returns_Tenant()
    {
        var client = _factory.CreateClient();
        var createRequest = new TenantCreateRequest("Slug Test Tenant", null, null);
        await client.PostAsJsonAsync("/api/v1/tenants", createRequest);

        var getResponse = await client.GetAsync("/api/v1/tenants/by-slug/slug-test-tenant");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var tenant = await getResponse.Content.ReadFromJsonAsync<TenantResponse>();
        Assert.NotNull(tenant);
        Assert.Equal("Slug Test Tenant", tenant.Name);
    }

    [Fact]
    public async Task Get_Nonexistent_Tenant_Returns_404_NotFound()
    {
        var client = _factory.CreateClient();
        var nonExistentId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/v1/tenants/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_Tenants_Returns_200_Ok()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/tenants");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private record TenantResponse(Guid Id, string Name, string Slug, string? Status);
}
