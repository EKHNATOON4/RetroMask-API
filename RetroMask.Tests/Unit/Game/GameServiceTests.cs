using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Dtos.Game;
using RetroMask.Application.Services.Game;
using RetroMask.Tests.Helpers;
using RetroMask.Domain.Entities.Game;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Domain.Entities.Sessions;
using RetroMask.Domain.Enums;
using RetroMask.Infrastructure.Persistence;
using RetroMask.Infrastructure.Services.Game;
using Xunit;

namespace RetroMask.Tests.Unit.Game;

public class GameServiceTests : IDisposable
{
    private readonly RetroMaskDbContext _context;
    private readonly IGameService _sut;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ISessionBroadcaster> _broadcasterMock = new();
    private readonly string _userId = "user-1";
    private readonly Guid _sessionId;

    public GameServiceTests()
    {
        var options = new DbContextOptionsBuilder<RetroMaskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new RetroMaskDbContext(options);

        _currentUserMock.Setup(u => u.UserId).Returns(_userId);

        var mapper = TestHelpers.CreateMapper();

        var uow = new UnitOfWork(_context);
        _sut = new GameService(uow, _currentUserMock.Object, mapper, _broadcasterMock.Object);

        _context.Users.Add(new ApplicationUser { Id = _userId, UserName = "user@test.com", Email = "user@test.com", DisplayName = "Test User" });

        var session = new Session
        {
            Title = "Test Session",
            TeamId = Guid.NewGuid(),
            FacilitatorId = _userId,
            Status = SessionStatus.Active
        };
        _sessionId = session.Id;
        _context.Set<Session>().Add(session);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetAvailableGames_ShouldReturn8Games()
    {
        var result = await _sut.GetAvailableGamesAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(8);
    }

    [Fact]
    public async Task StartGame_ShouldCreateGameAndBroadcast()
    {
        var request = new StartGameRequest { GameType = "two-truths-one-lie" };

        var result = await _sut.StartGameAsync(_sessionId, request);

        result.Success.Should().BeTrue();
        result.Data!.GameType.Should().Be("two-truths-one-lie");

        _broadcasterMock.Verify(b => b.BroadcastToSessionAsync(
            _sessionId, "GameStarted", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartGame_WhenActiveGameExists_ShouldFail()
    {
        _context.Set<IcebreakerGame>().Add(new IcebreakerGame
        {
            SessionId = _sessionId,
            GameType = "word-association",
            Title = "Word Association",
            IsCompleted = false,
            CreatedBy = _userId
        });
        await _context.SaveChangesAsync();

        var result = await _sut.StartGameAsync(_sessionId, new StartGameRequest { GameType = "emoji-mood" });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already");
    }

    [Fact]
    public async Task SubmitAnswer_ShouldCreateGameResult()
    {
        var game = new IcebreakerGame
        {
            SessionId = _sessionId,
            GameType = "two-truths-one-lie",
            Title = "Two Truths and a Lie",
            IsCompleted = false,
            CreatedBy = _userId
        };
        _context.Set<IcebreakerGame>().Add(game);
        await _context.SaveChangesAsync();

        var result = await _sut.SubmitAnswerAsync(game.Id,
            new SubmitAnswerRequest { Answer = "I climbed Everest" });

        result.Success.Should().BeTrue();

        var answers = await _context.Set<GameResult>().CountAsync(r => r.IcebreakerGameId == game.Id);
        answers.Should().Be(1);
    }

    [Fact]
    public async Task CompleteGame_ShouldSetIsCompletedAndBroadcast()
    {
        var game = new IcebreakerGame
        {
            SessionId = _sessionId,
            GameType = "emoji-mood",
            Title = "Emoji Mood Check",
            IsCompleted = false,
            CreatedBy = _userId
        };
        _context.Set<IcebreakerGame>().Add(game);
        await _context.SaveChangesAsync();

        var result = await _sut.CompleteGameAsync(game.Id);

        result.Success.Should().BeTrue();
        var updated = await _context.Set<IcebreakerGame>().FindAsync(game.Id);
        updated!.IsCompleted.Should().BeTrue();

        _broadcasterMock.Verify(b => b.BroadcastToSessionAsync(
            _sessionId, "GameCompleted", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLeaderboard_ShouldRankByScoreThenTime()
    {
        var game = new IcebreakerGame
        {
            SessionId = _sessionId,
            GameType = "word-association",
            Title = "Word Association",
            IsCompleted = true,
            CreatedBy = _userId
        };
        _context.Set<IcebreakerGame>().Add(game);

        var user2 = new ApplicationUser { Id = "user-2", UserName = "u2@t.com", Email = "u2@t.com", DisplayName = "User 2" };
        _context.Users.Add(user2);

        _context.Set<GameResult>().AddRange(
            new GameResult { IcebreakerGameId = game.Id, UserId = _userId, Score = 10, SubmittedAt = DateTime.UtcNow.AddSeconds(-5), Answer = "A1" },
            new GameResult { IcebreakerGameId = game.Id, UserId = "user-2", Score = 10, SubmittedAt = DateTime.UtcNow, Answer = "A2" }
        );
        await _context.SaveChangesAsync();

        var result = await _sut.GetLeaderboardAsync(game.Id);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.First().Rank.Should().Be(1);
    }

    public void Dispose() => _context.Dispose();
}
