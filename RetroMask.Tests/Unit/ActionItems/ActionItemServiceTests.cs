using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Dtos.ActionItems;
using RetroMask.Application.Services.ActionItems;
using RetroMask.Tests.Helpers;
using RetroMask.Application.Services.Notifications;
using RetroMask.Domain.Entities.ActionItems;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Domain.Enums;
using RetroMask.Infrastructure.Persistence;
using RetroMask.Infrastructure.Services.ActionItems;
using Xunit;

namespace RetroMask.Tests.Unit.ActionItems;

public class ActionItemServiceTests : IDisposable
{
    private readonly RetroMaskDbContext _context;
    private readonly IActionItemService _sut;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<INotificationService> _notificationMock = new();
    private readonly string _aliceId = "alice-id";
    private readonly string _bobId = "bob-id";

    public ActionItemServiceTests()
    {
        var options = new DbContextOptionsBuilder<RetroMaskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new RetroMaskDbContext(options);

        _currentUserMock.Setup(u => u.UserId).Returns(_aliceId);

        var mapper = TestHelpers.CreateMapper();

        var uow = new UnitOfWork(_context);
        _sut = new ActionItemService(uow, _currentUserMock.Object, mapper, _notificationMock.Object);

        _context.Users.AddRange(
            new ApplicationUser { Id = _aliceId, UserName = "alice@test.com", Email = "alice@test.com", DisplayName = "Alice" },
            new ApplicationUser { Id = _bobId, UserName = "bob@test.com", Email = "bob@test.com", DisplayName = "Bob" }
        );
        _context.SaveChanges();
    }

    [Fact]
    public async Task Create_ShouldCreateActionItemAndNotifyAssignee()
    {
        var sessionId = Guid.NewGuid();
        var request = new CreateActionItemRequest
        {
            Title = "Fix login bug",
            Description = "Login fails on mobile",
            SessionId = sessionId,
            AssignedToId = _bobId,
            Priority = ActionItemPriority.High,
            DueDate = DateTime.UtcNow.AddDays(7)
        };

        var result = await _sut.CreateAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.Title.Should().Be("Fix login bug");
        result.Data.AssignedToName.Should().Be("Bob");

        _notificationMock.Verify(n => n.SendAsync(
            _bobId,
            It.Is<Application.Dtos.Notifications.SendNotificationRequest>(r => r.Title == "New Action Item Assigned"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenAssignedToSelf_ShouldNotSendNotification()
    {
        var request = new CreateActionItemRequest
        {
            Title = "Self-assigned task",
            SessionId = Guid.NewGuid(),
            AssignedToId = _aliceId,
            Priority = ActionItemPriority.Medium
        };

        await _sut.CreateAsync(request);

        _notificationMock.Verify(n => n.SendAsync(
            It.IsAny<string>(),
            It.IsAny<Application.Dtos.Notifications.SendNotificationRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_WithStatusDone_ShouldSetCompletedAt()
    {
        var item = new ActionItem
        {
            Title = "Task",
            SessionId = Guid.NewGuid(),
            AssignedToId = _bobId,
            CreatedById = _aliceId,
            Priority = ActionItemPriority.Medium
        };
        _context.Set<ActionItem>().Add(item);
        await _context.SaveChangesAsync();

        var result = await _sut.UpdateAsync(item.Id, new UpdateActionItemRequest { Status = ActionItemStatus.Done });

        result.Success.Should().BeTrue();
        var updated = await _context.Set<ActionItem>().FindAsync(item.Id);
        updated!.CompletedAt.Should().NotBeNull();
        updated.Status.Should().Be(ActionItemStatus.Done);
    }

    [Fact]
    public async Task Delete_ShouldSoftDelete()
    {
        var item = new ActionItem
        {
            Title = "To Delete",
            SessionId = Guid.NewGuid(),
            AssignedToId = _aliceId,
            CreatedById = _aliceId,
            Priority = ActionItemPriority.Medium
        };
        _context.Set<ActionItem>().Add(item);
        await _context.SaveChangesAsync();

        var result = await _sut.DeleteAsync(item.Id);

        result.Success.Should().BeTrue();
        var deleted = await _context.Set<ActionItem>().IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == item.Id);
        deleted!.IsDeleted.Should().BeTrue();
        deleted.DeletedAt.Should().NotBeNull();
        deleted.DeletedBy.Should().Be(_aliceId);
    }

    [Fact]
    public async Task AddUpdate_WithStatusChange_ShouldUpdateItemStatus()
    {
        var item = new ActionItem
        {
            Title = "Task with updates",
            SessionId = Guid.NewGuid(),
            AssignedToId = _bobId,
            CreatedById = _aliceId,
            Priority = ActionItemPriority.Medium,
            Status = ActionItemStatus.Open
        };
        _context.Set<ActionItem>().Add(item);
        await _context.SaveChangesAsync();

        var result = await _sut.AddUpdateAsync(item.Id, new AddActionItemUpdateRequest
        {
            Note = "Done!",
            StatusChange = ActionItemStatus.Done,
            ProgressPercent = 100
        });

        result.Success.Should().BeTrue();
        result.Data!.ProgressPercent.Should().Be(100);

        var updated = await _context.Set<ActionItem>().FindAsync(item.Id);
        updated!.Status.Should().Be(ActionItemStatus.Done);
        updated.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ShouldFail()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    public void Dispose() => _context.Dispose();
}
