using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RetroMask.Tests.Integration;
using Xunit;

namespace RetroMask.Tests.Unit.Teams;

/// <summary>
/// Team service tests running via WebApplicationFactory (TeamService uses UserManager).
/// </summary>
public class TeamServiceTests : IClassFixture<RetroMaskWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly RetroMaskWebAppFactory _factory;

    public TeamServiceTests(RetroMaskWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> RegisterAndGetTokenAsync()
    {
        var email = $"team_{Guid.NewGuid():N}@test.com";
        var regBody = new
        {
            firstName = "Team",
            lastName = "Tester",
            email,
            password = "Password123!",
            confirmPassword = "Password123!"
        };
        var response = await _client.PostAsJsonAsync("/api/auth/register", regBody);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    [Fact]
    public async Task CreateTeam_WithValidRequest_ShouldReturnTeamDto()
    {
        var token = await RegisterAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new { name = "My Team", description = "A test team" };
        var response = await _client.PostAsJsonAsync("/api/teams", request);
        var json = await response.Content.ReadAsStringAsync();

        ((int)response.StatusCode).Should().BeInRange(200, 299, because: json);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("data").GetProperty("name").GetString().Should().Be("My Team");
    }

    [Fact]
    public async Task GetTeamById_WhenNotFound_ShouldIndicateFailure()
    {
        var token = await RegisterAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/teams/{Guid.NewGuid()}");
        var json = await response.Content.ReadAsStringAsync();

        // The API may return 200 with success=false or 404 — either is acceptable
        if (response.IsSuccessStatusCode)
        {
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
        }
        else
        {
            ((int)response.StatusCode).Should().BeOneOf(400, 404);
        }
    }
}
