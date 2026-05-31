using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RetroMask.Infrastructure.Persistence;
using Xunit;

namespace RetroMask.Tests.Integration;

/// <summary>
/// Custom factory that replaces SQL Server with InMemory DB.
/// A single shared DB name is used so the IdentitySeeder roles persist.
/// </summary>
public class RetroMaskWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"RetroMaskTestDb_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            // Remove all DbContextOptions registrations
            var descriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<RetroMaskDbContext>)
                          || d.ServiceType == typeof(DbContextOptions))
                .ToList();
            foreach (var d in descriptors) services.Remove(d);

            services.AddDbContext<RetroMaskDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }
}

/// <summary>
/// Base class for integration tests using an in-memory database.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<RetroMaskWebAppFactory>
{
    protected readonly RetroMaskWebAppFactory Factory;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(RetroMaskWebAppFactory factory)
    {
        Factory = factory;
        Client = Factory.CreateClient();
    }

    protected async Task<string> RegisterAndGetTokenAsync(string email, string password, string firstName = "Test", string lastName = "User")
    {
        var regBody = new { firstName, lastName, email, password, confirmPassword = password };
        var response = await Client.PostAsJsonAsync("/api/auth/register", regBody);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Register failed ({response.StatusCode}): {json}");

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    protected void SetAuth(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
}

public class AuthIntegrationTests : IntegrationTestBase
{
    public AuthIntegrationTests(RetroMaskWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task Register_ShouldReturnTokens()
    {
        var body = new
        {
            firstName = "Alice",
            lastName = "Test",
            email = $"alice_{Guid.NewGuid():N}@test.com",
            password = "Password123!",
            confirmPassword = "Password123!"
        };

        var response = await Client.PostAsJsonAsync("/api/auth/register", body);
        var json = await response.Content.ReadAsStringAsync();

        ((int)response.StatusCode).Should().BeInRange(200, 299, because: json);
        json.Should().Contain("accessToken");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldFail()
    {
        var email = $"fail_{Guid.NewGuid():N}@test.com";
        await RegisterAndGetTokenAsync(email, "Password123!");

        Client.DefaultRequestHeaders.Clear();
        var loginBody = new { email, password = "WrongPassword!" };
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginBody);
        var json = await response.Content.ReadAsStringAsync();

        json.Should().Contain("false");
    }

    [Fact]
    public async Task Me_WithValidToken_ShouldReturnProfile()
    {
        var email = $"me_{Guid.NewGuid():N}@test.com";
        var token = await RegisterAndGetTokenAsync(email, "Password123!");
        SetAuth(token);

        var response = await Client.GetAsync("/api/auth/me");
        ((int)response.StatusCode).Should().BeInRange(200, 299);

        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain(email);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ShouldReturn401()
    {
        Client.DefaultRequestHeaders.Clear();
        var response = await Client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

public class NotificationIntegrationTests : IntegrationTestBase
{
    public NotificationIntegrationTests(RetroMaskWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task GetNotifications_WhenEmpty_ShouldReturnEmptyList()
    {
        var token = await RegisterAndGetTokenAsync($"notif_{Guid.NewGuid():N}@test.com", "Password123!");
        SetAuth(token);

        var response = await Client.GetAsync("/api/notifications?page=1&pageSize=10");
        ((int)response.StatusCode).Should().BeInRange(200, 299);

        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("\"totalCount\":0");
    }

    [Fact]
    public async Task GetUnreadCount_ShouldReturnZeroInitially()
    {
        var token = await RegisterAndGetTokenAsync($"unread_{Guid.NewGuid():N}@test.com", "Password123!");
        SetAuth(token);

        var response = await Client.GetAsync("/api/notifications/unread-count");
        ((int)response.StatusCode).Should().BeInRange(200, 299);

        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("\"data\":0");
    }
}

public class InsightsIntegrationTests : IntegrationTestBase
{
    public InsightsIntegrationTests(RetroMaskWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task GetMyInsights_ShouldReturnInsightData()
    {
        var token = await RegisterAndGetTokenAsync($"insight_{Guid.NewGuid():N}@test.com", "Password123!");
        SetAuth(token);

        var response = await Client.GetAsync("/api/insights/me");
        ((int)response.StatusCode).Should().BeInRange(200, 299);
    }

    [Fact]
    public async Task GetGrowthSnapshots_ShouldReturnData()
    {
        var token = await RegisterAndGetTokenAsync($"growth_{Guid.NewGuid():N}@test.com", "Password123!");
        SetAuth(token);

        var response = await Client.GetAsync("/api/insights/me/growth?months=3");
        ((int)response.StatusCode).Should().BeInRange(200, 299);
    }
}
