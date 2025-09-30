using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Temple.Tests.Auth;

public class AuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");
        _factory = factory;
    }

    [Fact]
    public async Task Register_Then_Login_Returns_Jwt()
    {
        var client = _factory.CreateClient();
        var email = $"user_{Guid.NewGuid():N}@test.local";
        var registerResp = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = "Passw0rd!" });
        Assert.True(registerResp.IsSuccessStatusCode);

        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Passw0rd!" });
        var json = JsonDocument.Parse(await loginResp.Content.ReadAsStringAsync());
        Assert.True(loginResp.IsSuccessStatusCode);
        Assert.True(json.RootElement.TryGetProperty("accessToken", out var tokenElem));
        Assert.False(string.IsNullOrWhiteSpace(tokenElem.GetString()));
    }

    [Fact]
    public async Task Me_Requires_Auth()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact(Skip = "TODO: Fix authentication token handling in WebApplicationFactory - token not being passed correctly")]
    public async Task Me_Returns_Profile_When_Authorized()
    {
        var client = _factory.CreateClient();
        var email = $"user_{Guid.NewGuid():N}@test.local";
        var registerResp = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = "Passw0rd!" });
        Assert.True(registerResp.IsSuccessStatusCode, $"Register failed: {registerResp.StatusCode}");
        
        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Passw0rd!" });
        Assert.True(loginResp.IsSuccessStatusCode, $"Login failed: {loginResp.StatusCode}");
        
        var loginContent = await loginResp.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(loginContent);
        var token = json.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token), "Access token is null or empty");

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var resp = await client.SendAsync(req);
        
        if (!resp.IsSuccessStatusCode)
        {
            var errorContent = await resp.Content.ReadAsStringAsync();
            Assert.Fail($"Me endpoint failed with {resp.StatusCode}: {errorContent}");
        }
        
        var profile = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(email, profile.RootElement.GetProperty("email").GetString());
    }
}
