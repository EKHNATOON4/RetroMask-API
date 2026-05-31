using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Dtos.Feedback;
using RetroMask.Application.Services.Feedback;
using RetroMask.Application.Services.Notifications;
using RetroMask.Domain.Entities.Feedback;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Domain.Enums;
using RetroMask.Infrastructure.Persistence;
using RetroMask.Infrastructure.Services.Feedback;
using RetroMask.Tests.Helpers;
using Xunit;

namespace RetroMask.Tests.Unit.Feedback;

public class FeedbackServiceTests : IDisposable
{
    private readonly RetroMaskDbContext _context;
    private readonly IFeedbackService _sut;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<INotificationService> _notificationMock = new();
    private readonly string _aliceId = "alice-id";
    private readonly string _bobId = "bob-id";

    public FeedbackServiceTests()
    {
        var options = new DbContextOptionsBuilder<RetroMaskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new RetroMaskDbContext(options);

        _currentUserMock.Setup(u => u.UserId).Returns(_aliceId);
        _currentUserMock.Setup(u => u.DisplayName).Returns("Alice");

        var mapper = TestHelpers.CreateMapper();
        var uow = new UnitOfWork(_context);
        _sut = new FeedbackService(uow, _currentUserMock.Object, mapper, _notificationMock.Object);

        SeedUsers();
    }

    private void SeedUsers()
    {
        _context.Users.AddRange(
            new ApplicationUser { Id = _aliceId, UserName = "alice@test.com", Email = "alice@test.com", DisplayName = "Alice" },
            new ApplicationUser { Id = _bobId, UserName = "bob@test.com", Email = "bob@test.com", DisplayName = "Bob" }
        );
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateFeedback_WithPraiseContent_ShouldClassifyAsPraise()
    {
        var request = new CreateFeedbackRequest
        {
            ReceiverId = _bobId,
            Content = "Amazing work! You did a great job on the project.",
            IsAnonymous = false,
            SessionId = Guid.NewGuid()
        };

        var result = await _sut.CreateFeedbackAsync(request);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.FeedbackType.Should().Be(FeedbackType.Praise);
    }

    [Fact]
    public async Task CreateFeedback_WithConstructiveContent_ShouldClassifyAsConstructive()
    {
        var request = new CreateFeedbackRequest
        {
            ReceiverId = _bobId,
            Content = "I think you could improve by focusing more on testing. Consider adding more unit tests.",
            IsAnonymous = false,
            SessionId = Guid.NewGuid()
        };

        var result = await _sut.CreateFeedbackAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.FeedbackType.Should().Be(FeedbackType.Constructive);
    }

    [Fact]
    public async Task CreateFeedback_WithToxicContent_ShouldBeBlocked()
    {
        var request = new CreateFeedbackRequest
        {
            ReceiverId = _bobId,
            Content = "You are an idiot and completely useless at your job",
            IsAnonymous = false,
            SessionId = Guid.NewGuid()
        };

        var result = await _sut.CreateFeedbackAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("inappropriate");
    }

    [Fact]
    public async Task CreateFeedback_ShouldSendNotificationToReceiver()
    {
        var request = new CreateFeedbackRequest
        {
            ReceiverId = _bobId,
            Content = "Great collaboration!",
            IsAnonymous = false,
            SessionId = Guid.NewGuid()
        };

        await _sut.CreateFeedbackAsync(request);

        _notificationMock.Verify(n => n.SendAsync(
            _bobId,
            It.IsAny<Application.Dtos.Notifications.SendNotificationRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateFeedback_ToSelf_ShouldFail()
    {
        var request = new CreateFeedbackRequest
        {
            ReceiverId = _aliceId,
            Content = "Self feedback",
            SessionId = Guid.NewGuid()
        };

        var result = await _sut.CreateFeedbackAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("yourself");
    }

    [Fact]
    public async Task GetReceivedFeedback_ShouldReturnFeedbackForReceiver()
    {
        _context.Set<FriendFeedback>().Add(new FriendFeedback
        {
            GiverId = _aliceId,
            ReceiverId = _bobId,
            Content = "Test feedback",
            SessionId = Guid.NewGuid(),
            FeedbackType = FeedbackType.General
        });
        await _context.SaveChangesAsync();

        _currentUserMock.Setup(u => u.UserId).Returns(_bobId);
        var uow = new UnitOfWork(_context);
        var sut = new FeedbackService(uow, _currentUserMock.Object, TestHelpers.CreateMapper(), _notificationMock.Object);

        var result = await sut.GetReceivedFeedbackAsync(1, 10);

        result.Success.Should().BeTrue();
        result.Data!.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteFeedback_ByNonGiver_ShouldFail()
    {
        var fb = new FriendFeedback
        {
            GiverId = _bobId,
            ReceiverId = _aliceId,
            Content = "Feedback from Bob",
            SessionId = Guid.NewGuid()
        };
        _context.Set<FriendFeedback>().Add(fb);
        await _context.SaveChangesAsync();

        var result = await _sut.DeleteFeedbackAsync(fb.Id);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("giver");
    }

    public void Dispose() => _context.Dispose();
}
