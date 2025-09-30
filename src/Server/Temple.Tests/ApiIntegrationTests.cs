using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Temple.Application.Tenants;

namespace Temple.Tests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");
        _factory = factory;
    }

    [Fact]
    public async Task Root_Redirects_To_Swagger()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var resp = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Equal("/swagger", resp.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Health_Returns_OK()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health");
        Assert.True(resp.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Can_Create_And_Get_Tenant()
    {
        var client = _factory.CreateClient();
    // Updated signature requires TaxonomyId and optional ReligionId; pass nulls for simple creation in test
    var created = await client.PostAsJsonAsync("/api/v1/tenants", new TenantCreateRequest("Integration Tenant", null, null));
        created.EnsureSuccessStatusCode();
        var createdObj = await created.Content.ReadFromJsonAsync<TenantResponse>();
        Assert.NotNull(createdObj);
        var get = await client.GetAsync($"/api/v1/tenants/{createdObj!.Id}");
        get.EnsureSuccessStatusCode();
    }

    private record TenantResponse(Guid Id, string Name, string Slug, string? Status);
}
