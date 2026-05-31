using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Dtos.Voting;
using RetroMask.Application.Services.Voting;
using RetroMask.Domain.Entities.Discussion;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Domain.Entities.Sessions;
using RetroMask.Domain.Entities.Voting;
using RetroMask.Domain.Enums;
using RetroMask.Infrastructure.Persistence;
using RetroMask.Infrastructure.Services.Voting;
using Xunit;

namespace RetroMask.Tests.Unit.Voting;

public class VotingServiceTests : IDisposable
{
    private readonly RetroMaskDbContext _context;
    private readonly IVotingService _sut;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ISessionBroadcaster> _broadcasterMock = new();
    private readonly string _userId = "user-1";
    private Guid _sessionId;
    private Guid _phaseId;
    private Guid _pointId;

    public VotingServiceTests()
    {
        var options = new DbContextOptionsBuilder<RetroMaskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new RetroMaskDbContext(options);

        _currentUserMock.Setup(u => u.UserId).Returns(_userId);

        var uow = new UnitOfWork(_context);
        _sut = new VotingService(uow, _currentUserMock.Object, _broadcasterMock.Object);

        SeedData();
    }

    private void SeedData()
    {
        _context.Users.Add(new ApplicationUser { Id = _userId, UserName = "user@test.com", Email = "user@test.com" });

        var session = new Session
        {
            Title = "Test", TeamId = Guid.NewGuid(), FacilitatorId = _userId,
            Status = SessionStatus.Active, VotingEnabled = true, MaxVotesPerUser = 2
        };
        _sessionId = session.Id;
        _context.Set<Session>().Add(session);

        var phase = new SessionPhase { SessionId = _sessionId, Title = "Phase 1", PhaseType = SessionPhaseType.WentWell, Order = 1, Status = PhaseStatus.Active };
        _phaseId = phase.Id;
        _context.Set<SessionPhase>().Add(phase);

        var point = new DiscussionPoint { PhaseId = _phaseId, AuthorId = _userId, Content = "Good work!" };
        _pointId = point.Id;
        _context.Set<DiscussionPoint>().Add(point);

        _context.SaveChanges();
    }

    [Fact]
    public async Task CastVote_ShouldCreateVoteAndRecalculateSummary()
    {
        var request = new CastVoteRequest { VoteType = VoteType.Up };

        var result = await _sut.CastVoteAsync(_pointId, request);

        result.Success.Should().BeTrue();
        result.Data!.UpVotes.Should().Be(1);
        result.Data.MyVote.Should().Be(VoteType.Up);
    }

    [Fact]
    public async Task CastVote_WhenUserAlreadyVotedSameType_ShouldFail()
    {
        await _sut.CastVoteAsync(_pointId, new CastVoteRequest { VoteType = VoteType.Up });

        var result = await _sut.CastVoteAsync(_pointId, new CastVoteRequest { VoteType = VoteType.Up });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already cast");
    }

    [Fact]
    public async Task CastVote_WhenUserChangesVote_ShouldUpdate()
    {
        await _sut.CastVoteAsync(_pointId, new CastVoteRequest { VoteType = VoteType.Up });

        var result = await _sut.CastVoteAsync(_pointId, new CastVoteRequest { VoteType = VoteType.Down });

        result.Success.Should().BeTrue();
        result.Data!.DownVotes.Should().Be(1);
        result.Data.UpVotes.Should().Be(0);
    }

    [Fact]
    public async Task CastVote_WhenExceedsMaxVotes_ShouldFail()
    {
        // Session allows 2 max votes. Create 3 points, try to vote on all 3
        var point2 = new DiscussionPoint { PhaseId = _phaseId, AuthorId = _userId, Content = "Point 2" };
        var point3 = new DiscussionPoint { PhaseId = _phaseId, AuthorId = _userId, Content = "Point 3" };
        _context.Set<DiscussionPoint>().AddRange(point2, point3);
        await _context.SaveChangesAsync();

        await _sut.CastVoteAsync(_pointId, new CastVoteRequest { VoteType = VoteType.Up });
        await _sut.CastVoteAsync(point2.Id, new CastVoteRequest { VoteType = VoteType.Up });

        var result = await _sut.CastVoteAsync(point3.Id, new CastVoteRequest { VoteType = VoteType.Up });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("budget exhausted");
    }

    [Fact]
    public async Task CastVote_WhenVotingDisabled_ShouldFail()
    {
        var session = await _context.Set<Session>().FindAsync(_sessionId);
        session!.VotingEnabled = false;
        await _context.SaveChangesAsync();

        var result = await _sut.CastVoteAsync(_pointId, new CastVoteRequest { VoteType = VoteType.Up });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("disabled");
    }

    [Fact]
    public async Task CastVote_WhenVotingClosed_ShouldFail()
    {
        _context.Set<VoteSummary>().Add(new VoteSummary
        {
            DiscussionPointId = _pointId,
            Status = VoteSummaryStatus.Closed,
            ClosedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _sut.CastVoteAsync(_pointId, new CastVoteRequest { VoteType = VoteType.Up });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("closed");
    }

    [Fact]
    public async Task RemoveVote_ShouldDeleteVoteAndRecalculate()
    {
        await _sut.CastVoteAsync(_pointId, new CastVoteRequest { VoteType = VoteType.Up });

        var result = await _sut.RemoveVoteAsync(_pointId);

        result.Success.Should().BeTrue();
        var votes = await _context.Set<Vote>().CountAsync(v => v.DiscussionPointId == _pointId);
        votes.Should().Be(0);
    }

    [Fact]
    public async Task CloseVoting_ShouldSetClosedStatus()
    {
        var result = await _sut.CloseVotingAsync(_pointId);

        result.Success.Should().BeTrue();
        var summary = await _context.Set<VoteSummary>().FirstOrDefaultAsync(s => s.DiscussionPointId == _pointId);
        summary!.Status.Should().Be(VoteSummaryStatus.Closed);
    }

    [Fact]
    public async Task GetVoteSummary_ShouldReturnCorrectCounts()
    {
        await _sut.CastVoteAsync(_pointId, new CastVoteRequest { VoteType = VoteType.Up });

        var result = await _sut.GetVoteSummaryAsync(_pointId);

        result.Success.Should().BeTrue();
        result.Data!.UpVotes.Should().Be(1);
        result.Data.DownVotes.Should().Be(0);
        result.Data.MyVote.Should().Be(VoteType.Up);
    }

    public void Dispose() => _context.Dispose();
}
