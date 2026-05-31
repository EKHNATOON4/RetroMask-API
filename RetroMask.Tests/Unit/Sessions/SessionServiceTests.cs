using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Dtos.Sessions;
using RetroMask.Application.Services.Sessions;
using RetroMask.Tests.Helpers;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Domain.Entities.Sessions;
using RetroMask.Domain.Entities.Teams;
using RetroMask.Domain.Enums;
using RetroMask.Infrastructure.Persistence;
using RetroMask.Infrastructure.Services.Sessions;
using Xunit;

namespace RetroMask.Tests.Unit.Sessions;

public class SessionServiceTests : IDisposable
{
    private readonly RetroMaskDbContext _context;
    private readonly ISessionService _sut;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ISessionBroadcaster> _broadcasterMock = new();
    private readonly string _userId = "user-1";
    private readonly Guid _teamId;

    public SessionServiceTests()
    {
        var options = new DbContextOptionsBuilder<RetroMaskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new RetroMaskDbContext(options);

        _currentUserMock.Setup(u => u.UserId).Returns(_userId);

        var mapper = TestHelpers.CreateMapper();

        var uow = new UnitOfWork(_context);
        _sut = new SessionService(uow, _currentUserMock.Object, mapper, _broadcasterMock.Object);

        // Seed user and team
        _context.Users.Add(new ApplicationUser { Id = _userId, UserName = "user@test.com", Email = "user@test.com", DisplayName = "Test User" });
        var team = new Team { Name = "Test Team", CreatedBy = _userId };
        _teamId = team.Id;
        _context.Set<Team>().Add(team);
        _context.Set<TeamMember>().Add(new TeamMember { TeamId = _teamId, UserId = _userId, Role = TeamMemberRole.Admin, IsActive = true });
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateSession_WithValidTeam_ShouldReturnSessionDto()
    {
        var request = new CreateSessionRequest
        {
            Title = "Sprint 42 Retro",
            TeamId = _teamId,
            MaxVotesPerUser = 5
        };

        var result = await _sut.CreateSessionAsync(request);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be("Sprint 42 Retro");
        result.Data.Status.Should().Be(SessionStatus.Draft);
        result.Data.MaxVotesPerUser.Should().Be(5);
    }

    [Fact]
    public async Task CreateSession_WithNonExistentTeam_ShouldFail()
    {
        var request = new CreateSessionRequest
        {
            Title = "Test",
            TeamId = Guid.NewGuid(),
            MaxVotesPerUser = 3
        };

        var result = await _sut.CreateSessionAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Team not found");
    }

    [Fact]
    public async Task CreateSession_ShouldCreateDefaultPhases()
    {
        var request = new CreateSessionRequest { Title = "With Phases", TeamId = _teamId };

        var result = await _sut.CreateSessionAsync(request);

        result.Success.Should().BeTrue();
        var phases = await _context.Set<SessionPhase>()
            .Where(p => p.SessionId == result.Data!.Id)
            .OrderBy(p => p.Order)
            .ToListAsync();
        phases.Should().HaveCount(5);
    }

    [Fact]
    public async Task StartSession_ShouldActivateFirstPhaseAndBroadcast()
    {
        var createReq = new CreateSessionRequest { Title = "Start Test", TeamId = _teamId };
        var created = await _sut.CreateSessionAsync(createReq);

        var result = await _sut.StartSessionAsync(created.Data!.Id);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(SessionStatus.Active);

        var activePhase = await _context.Set<SessionPhase>()
            .FirstOrDefaultAsync(p => p.SessionId == created.Data.Id && p.Status == PhaseStatus.Active);
        activePhase.Should().NotBeNull();

        _broadcasterMock.Verify(b => b.BroadcastToSessionAsync(
            created.Data.Id, "SessionStarted", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartSession_WhenAlreadyActive_ShouldFail()
    {
        var createReq = new CreateSessionRequest { Title = "Active Test", TeamId = _teamId };
        var created = await _sut.CreateSessionAsync(createReq);
        await _sut.StartSessionAsync(created.Data!.Id);

        var result = await _sut.StartSessionAsync(created.Data.Id);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task JoinSession_ShouldSucceedForAnonymousSession()
    {
        var createReq = new CreateSessionRequest { Title = "Anonymous", TeamId = _teamId, IsAnonymous = true };
        var created = await _sut.CreateSessionAsync(createReq);

        var result = await _sut.JoinSessionAsync(created.Data!.Id, new JoinSessionRequest());

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteSession_ShouldAutoCompleteActivePhases()
    {
        var createReq = new CreateSessionRequest { Title = "Complete Test", TeamId = _teamId };
        var created = await _sut.CreateSessionAsync(createReq);
        await _sut.StartSessionAsync(created.Data!.Id);

        var result = await _sut.CompleteSessionAsync(created.Data.Id);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(SessionStatus.Completed);

        var activePhases = await _context.Set<SessionPhase>()
            .CountAsync(p => p.SessionId == created.Data.Id && p.Status == PhaseStatus.Active);
        activePhases.Should().Be(0);
    }

    [Fact]
    public async Task DeleteSession_ShouldSoftDelete()
    {
        var createReq = new CreateSessionRequest { Title = "Delete Test", TeamId = _teamId };
        var created = await _sut.CreateSessionAsync(createReq);

        var result = await _sut.DeleteSessionAsync(created.Data!.Id);

        result.Success.Should().BeTrue();
        var deleted = await _context.Set<Session>().IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == created.Data.Id);
        deleted!.IsDeleted.Should().BeTrue();
    }

    public void Dispose() => _context.Dispose();
}
