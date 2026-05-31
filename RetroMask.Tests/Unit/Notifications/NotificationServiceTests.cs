using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Dtos.Notifications;
using RetroMask.Application.Services.Notifications;
using RetroMask.Tests.Helpers;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Domain.Entities.Notifications;
using RetroMask.Domain.Enums;
using RetroMask.Infrastructure.Persistence;
using RetroMask.Infrastructure.Services.Notifications;
using Xunit;

namespace RetroMask.Tests.Unit.Notifications;

public class NotificationServiceTests : IDisposable
{
    private readonly RetroMaskDbContext _context;
    private readonly INotificationService _sut;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ISessionBroadcaster> _broadcasterMock = new();
    private readonly string _userId = "user-1";

    public NotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<RetroMaskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new RetroMaskDbContext(options);

        _currentUserMock.Setup(u => u.UserId).Returns(_userId);

        var mapper = TestHelpers.CreateMapper();

        var uow = new UnitOfWork(_context);
        _sut = new NotificationService(uow, _currentUserMock.Object, mapper, _broadcasterMock.Object);

        _context.Users.Add(new ApplicationUser { Id = _userId, UserName = "test@test.com", Email = "test@test.com" });
        _context.SaveChanges();
    }

    [Fact]
    public async Task SendAsync_ShouldCreateNotificationAndBroadcast()
    {
        var request = new SendNotificationRequest
        {
            Type = NotificationType.FeedbackReceived,
            Title = "New Feedback",
            Message = "You received feedback",
            ActionUrl = "/feedback/1"
        };

        await _sut.SendAsync(_userId, request);

        var count = await _context.Set<Notification>().CountAsync();
        count.Should().Be(1);

        _broadcasterMock.Verify(b => b.BroadcastToUserAsync(
            _userId, "NotificationReceived", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUnreadCount_ShouldReturnCorrectCount()
    {
        _context.Set<Notification>().AddRange(
            new Notification { UserId = _userId, Title = "N1", Message = "M1", IsRead = false, Type = NotificationType.FeedbackReceived },
            new Notification { UserId = _userId, Title = "N2", Message = "M2", IsRead = false, Type = NotificationType.ActionItemAssigned },
            new Notification { UserId = _userId, Title = "N3", Message = "M3", IsRead = true, Type = NotificationType.FeedbackReceived }
        );
        await _context.SaveChangesAsync();

        var result = await _sut.GetUnreadCountAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().Be(2);
    }

    [Fact]
    public async Task MarkAsRead_ShouldSetIsReadAndReadAt()
    {
        var notification = new Notification
        {
            UserId = _userId, Title = "Test", Message = "Test",
            IsRead = false, Type = NotificationType.FeedbackReceived
        };
        _context.Set<Notification>().Add(notification);
        await _context.SaveChangesAsync();

        var result = await _sut.MarkAsReadAsync(notification.Id);

        result.Success.Should().BeTrue();
        var updated = await _context.Set<Notification>().FindAsync(notification.Id);
        updated!.IsRead.Should().BeTrue();
        updated.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAllAsRead_ShouldMarkAllUnreadNotifications()
    {
        _context.Set<Notification>().AddRange(
            new Notification { UserId = _userId, Title = "N1", Message = "M1", IsRead = false, Type = NotificationType.FeedbackReceived },
            new Notification { UserId = _userId, Title = "N2", Message = "M2", IsRead = false, Type = NotificationType.ActionItemAssigned }
        );
        await _context.SaveChangesAsync();

        var result = await _sut.MarkAllAsReadAsync();

        result.Success.Should().BeTrue();
        var unread = await _context.Set<Notification>().CountAsync(n => !n.IsRead && n.UserId == _userId);
        unread.Should().Be(0);
    }

    [Fact]
    public async Task MarkAsRead_WithWrongUser_ShouldFail()
    {
        var notification = new Notification
        {
            UserId = "other-user", Title = "Test", Message = "Test",
            IsRead = false, Type = NotificationType.FeedbackReceived
        };
        _context.Set<Notification>().Add(notification);
        await _context.SaveChangesAsync();

        var result = await _sut.MarkAsReadAsync(notification.Id);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetMyNotifications_ShouldReturnPagedResults()
    {
        for (int i = 0; i < 15; i++)
        {
            _context.Set<Notification>().Add(new Notification
            {
                UserId = _userId, Title = $"N{i}", Message = $"M{i}",
                Type = NotificationType.FeedbackReceived
            });
        }
        await _context.SaveChangesAsync();

        var result = await _sut.GetMyNotificationsAsync(1, 10);

        result.Success.Should().BeTrue();
        result.Data!.TotalCount.Should().Be(15);
        result.Data.Items.Count().Should().Be(10);
        result.Data.TotalPages.Should().Be(2);
    }

    public void Dispose() => _context.Dispose();
}
