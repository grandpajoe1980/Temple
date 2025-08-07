using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Temple.Tests.Auth;

public class AuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public AuthTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Register_Then_Login_Returns_Jwt()
    {
        var client = _factory.CreateClient();
        var email = $"user_{Guid.NewGuid():N}@test.local";
        var registerResp = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "Passw0rd!" });
        Assert.True(registerResp.IsSuccessStatusCode);

        var loginResp = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Passw0rd!" });
        var json = JsonDocument.Parse(await loginResp.Content.ReadAsStringAsync());
        Assert.True(loginResp.IsSuccessStatusCode);
        Assert.True(json.RootElement.TryGetProperty("accessToken", out var tokenElem));
        Assert.False(string.IsNullOrWhiteSpace(tokenElem.GetString()));
    }
}
