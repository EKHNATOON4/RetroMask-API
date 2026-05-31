using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using System.Text.Json;
using RetroMask.Tests.Integration;
using Xunit;

namespace RetroMask.Tests.Unit.Auth;

/// <summary>
/// Auth service tests running via WebApplicationFactory (Identity requires the full host pipeline).
/// </summary>
public class AuthServiceTests : IClassFixture<RetroMaskWebAppFactory>
{
    private readonly HttpClient _client;

    public AuthServiceTests(RetroMaskWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidRequest_ShouldReturnAuthResponse()
    {
        var request = new
        {
            firstName = "Test",
            lastName = "User",
            email = $"auth_reg_{Guid.NewGuid():N}@test.com",
            password = "Test@1234",
            confirmPassword = "Test@1234"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        var json = await response.Content.ReadAsStringAsync();

        ((int)response.StatusCode).Should().BeInRange(200, 299, because: json);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        root.GetProperty("data").GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        root.GetProperty("data").GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldFail()
    {
        var loginBody = new { email = "nonexistent@example.com", password = "WrongPass123!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginBody);
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
    }
}
