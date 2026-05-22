using FluentAssertions;
using Moq;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Abstractions.Repositories;
using RetroMask.Application.Dtos.Teams;
using RetroMask.Application.Services.Teams;
using Xunit;

namespace RetroMask.Tests.Unit.Teams;

public class TeamServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    [Fact(Skip = "Pending implementation")]
    public async Task CreateTeam_WithValidRequest_ShouldReturnTeamDto()
    {
        // Arrange
        _currentUserMock.Setup(u => u.UserId).Returns("user-123");
        var request = new CreateTeamRequest { Name = "My Team", Description = "A test team" };

        // Act & Assert
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation")]
    public async Task GetTeamById_WhenNotFound_ShouldThrowNotFoundException()
    {
        // Arrange & Act & Assert
        await Task.CompletedTask;
    }
}
