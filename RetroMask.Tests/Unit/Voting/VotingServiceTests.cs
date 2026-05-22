using FluentAssertions;
using Moq;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Abstractions.Repositories;
using RetroMask.Application.Dtos.Voting;
using RetroMask.Domain.Enums;
using Xunit;

namespace RetroMask.Tests.Unit.Voting;

public class VotingServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ISessionBroadcaster> _broadcasterMock = new();

    [Fact(Skip = "Pending implementation")]
    public async Task CastVote_WhenUserAlreadyVoted_ShouldUpdateExistingVote()
    {
        var request = new CastVoteRequest { VoteType = VoteType.Up };
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation")]
    public async Task CastVote_WhenExceedsMaxVotes_ShouldFail()
    {
        await Task.CompletedTask;
    }
}
