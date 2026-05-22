using FluentAssertions;
using Moq;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Abstractions.Repositories;
using RetroMask.Application.Dtos.Sessions;
using Xunit;

namespace RetroMask.Tests.Unit.Sessions;

public class SessionServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ISessionBroadcaster> _broadcasterMock = new();

    [Fact(Skip = "Pending implementation")]
    public async Task CreateSession_WithValidTeam_ShouldReturnSessionDto()
    {
        var request = new CreateSessionRequest
        {
            Title = "Sprint 42 Retro",
            TeamId = Guid.NewGuid(),
            MaxVotesPerUser = 3
        };
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation")]
    public async Task StartSession_WhenAlreadyActive_ShouldFail()
    {
        await Task.CompletedTask;
    }
}
