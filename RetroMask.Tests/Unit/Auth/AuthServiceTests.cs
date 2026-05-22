using FluentAssertions;
using Moq;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Abstractions.Repositories;
using RetroMask.Application.Dtos.Auth;
using RetroMask.Application.Services.Auth;
using Xunit;

namespace RetroMask.Tests.Unit.Auth;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IJwtTokenService> _jwtMock = new();
    private readonly Mock<IEmailService> _emailMock = new();

    // TODO: Inject real IAuthService implementation once created in Application layer
    // private readonly IAuthService _sut;

    [Fact(Skip = "Pending implementation")]
    public async Task Register_WithValidRequest_ShouldReturnAuthResponse()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Password = "Test@1234",
            ConfirmPassword = "Test@1234"
        };

        // Act
        // var result = await _sut.RegisterAsync(request);

        // Assert
        // result.Success.Should().BeTrue();
        // result.Data.Should().NotBeNull();
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation")]
    public async Task Login_WithInvalidCredentials_ShouldFail()
    {
        // Arrange
        var request = new LoginRequest { Email = "wrong@example.com", Password = "WrongPass" };

        // Act & Assert
        await Task.CompletedTask;
    }
}
