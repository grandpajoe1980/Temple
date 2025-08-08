using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Temple.Application.Tenants;
using Temple.Infrastructure.Persistence;

namespace Temple.Tests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory) =>
        _factory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((ctx, cfg) =>
            {
                var dict = new Dictionary<string, string?> { { "UseInMemoryDatabase", "true" } };
                cfg.AddInMemoryCollection(dict!);
            }));

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
        var created = await client.PostAsJsonAsync("/api/tenants", new TenantCreateRequest("Integration Tenant"));
        created.EnsureSuccessStatusCode();
        var createdObj = await created.Content.ReadFromJsonAsync<TenantResponse>();
        Assert.NotNull(createdObj);
        var get = await client.GetAsync($"/api/tenants/{createdObj!.Id}");
        get.EnsureSuccessStatusCode();
    }

    private record TenantResponse(Guid Id, string Name, string Slug, string? Status);
}
