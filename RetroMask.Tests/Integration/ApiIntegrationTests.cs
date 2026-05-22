using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RetroMask.API.Controllers.Auth;
using RetroMask.Infrastructure.Persistence;
using Xunit;

namespace RetroMask.Tests.Integration;

/// <summary>
/// Base class for integration tests using an in-memory database.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<WebApplicationFactory<AuthController>>
{
    protected readonly WebApplicationFactory<AuthController> Factory;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(WebApplicationFactory<AuthController> factory)
    {
        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace SQL Server with InMemory for tests
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<RetroMaskDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<RetroMaskDbContext>(options =>
                    options.UseInMemoryDatabase("RetroMaskTestDb"));
            });
        });

        Client = Factory.CreateClient();
    }
}

public class HealthCheckTests : IntegrationTestBase
{
    public HealthCheckTests(WebApplicationFactory<AuthController> factory) : base(factory) { }

    [Fact(Skip = "Requires running API")]
    public async Task Swagger_ShouldBeAccessible()
    {
        var response = await Client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
    }
}
