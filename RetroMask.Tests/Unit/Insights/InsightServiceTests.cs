using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Services.Insights;
using RetroMask.Tests.Helpers;
using RetroMask.Domain.Entities.ActionItems;
using RetroMask.Domain.Entities.Discussion;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Domain.Entities.Insights;
using RetroMask.Domain.Entities.Sessions;
using RetroMask.Domain.Entities.Voting;
using RetroMask.Domain.Enums;
using RetroMask.Infrastructure.Persistence;
using RetroMask.Infrastructure.Services.Insights;
using Xunit;

namespace RetroMask.Tests.Unit.Insights;

public class InsightServiceTests : IDisposable
{
    private readonly RetroMaskDbContext _context;
    private readonly IInsightService _sut;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly string _userId = "user-1";

    public InsightServiceTests()
    {
        var options = new DbContextOptionsBuilder<RetroMaskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new RetroMaskDbContext(options);

        _currentUserMock.Setup(u => u.UserId).Returns(_userId);

        var mapper = TestHelpers.CreateMapper();

        var uow = new UnitOfWork(_context);
        _sut = new InsightService(uow, _currentUserMock.Object, mapper);

        _context.Users.Add(new ApplicationUser { Id = _userId, UserName = "user@test.com", Email = "user@test.com", DisplayName = "Test" });
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetMyInsights_WhenNoCache_ShouldGenerateAndReturn()
    {
        var session = new Session { Title = "S1", TeamId = Guid.NewGuid(), FacilitatorId = _userId, Status = SessionStatus.Active };
        _context.Set<Session>().Add(session);

        var phase = new SessionPhase { SessionId = session.Id, Title = "Phase 1", PhaseType = SessionPhaseType.WentWell, Order = 1 };
        _context.Set<SessionPhase>().Add(phase);

        _context.Set<SessionMember>().Add(new SessionMember { SessionId = session.Id, UserId = _userId, JoinedAt = DateTime.UtcNow });
        _context.Set<DiscussionPoint>().Add(new DiscussionPoint { PhaseId = phase.Id, AuthorId = _userId, Content = "Point 1" });
        await _context.SaveChangesAsync();

        var result = await _sut.GetMyInsightsAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TotalSessionsAttended.Should().Be(1);
        result.Data.TotalPointsSubmitted.Should().Be(1);
    }

    [Fact]
    public async Task GetMyInsights_WithFreshCache_ShouldReturnCachedData()
    {
        var insight = new UserInsight
        {
            UserId = _userId,
            TotalSessionsAttended = 5,
            TotalPointsSubmitted = 20,
            TotalVotesCast = 10,
            AverageEngagementScore = 75.0,
            PeriodStart = DateTime.UtcNow.AddMonths(-6),
            PeriodEnd = DateTime.UtcNow
        };
        _context.Set<UserInsight>().Add(insight);
        await _context.SaveChangesAsync();

        var result = await _sut.GetMyInsightsAsync();

        result.Success.Should().BeTrue();
        result.Data!.TotalSessionsAttended.Should().Be(5);
        result.Data.TotalPointsSubmitted.Should().Be(20);
    }

    [Fact]
    public async Task RefreshInsights_ShouldUpdateExistingInsight()
    {
        var insight = new UserInsight
        {
            UserId = _userId,
            TotalSessionsAttended = 0,
            PeriodStart = DateTime.UtcNow.AddMonths(-6),
            PeriodEnd = DateTime.UtcNow.AddHours(-25)
        };
        _context.Set<UserInsight>().Add(insight);

        var session = new Session { Title = "S1", TeamId = Guid.NewGuid(), FacilitatorId = _userId, Status = SessionStatus.Active };
        _context.Set<Session>().Add(session);
        _context.Set<SessionMember>().Add(new SessionMember { SessionId = session.Id, UserId = _userId, JoinedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        await _sut.RefreshInsightsAsync(_userId);

        var updated = await _context.Set<UserInsight>().FirstOrDefaultAsync(i => i.UserId == _userId);
        updated!.TotalSessionsAttended.Should().Be(1);
    }

    [Fact]
    public async Task GetGrowthSnapshots_ShouldGenerateIfEmpty()
    {
        var result = await _sut.GetGrowthSnapshotsAsync(6);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(6);
    }

    [Fact]
    public async Task EngagementScore_ShouldCalculateCorrectly()
    {
        var session = new Session { Title = "S1", TeamId = Guid.NewGuid(), FacilitatorId = _userId, Status = SessionStatus.Active };
        _context.Set<Session>().Add(session);

        var phase = new SessionPhase { SessionId = session.Id, Title = "Phase 1", PhaseType = SessionPhaseType.WentWell, Order = 1 };
        _context.Set<SessionPhase>().Add(phase);

        _context.Set<SessionMember>().Add(new SessionMember { SessionId = session.Id, UserId = _userId, JoinedAt = DateTime.UtcNow });

        for (int i = 0; i < 3; i++)
        {
            _context.Set<DiscussionPoint>().Add(new DiscussionPoint { PhaseId = phase.Id, AuthorId = _userId, Content = $"P{i}" });
        }

        _context.Set<ActionItem>().Add(new ActionItem
        {
            Title = "Done task", SessionId = session.Id, AssignedToId = _userId,
            CreatedById = _userId, Status = ActionItemStatus.Done, Priority = ActionItemPriority.High
        });
        await _context.SaveChangesAsync();

        await _sut.RefreshInsightsAsync(_userId);

        var insight = await _context.Set<UserInsight>().FirstAsync(i => i.UserId == _userId);
        insight.TotalSessionsAttended.Should().Be(1);
        insight.TotalPointsSubmitted.Should().Be(3);
        insight.TotalActionItemsCompleted.Should().Be(1);
        insight.AverageEngagementScore.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RefreshMyInsights_ShouldReturnUpdatedDto()
    {
        var result = await _sut.RefreshMyInsightsAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    public void Dispose() => _context.Dispose();
}
