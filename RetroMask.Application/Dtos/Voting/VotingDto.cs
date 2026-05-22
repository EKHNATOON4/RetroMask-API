using RetroMask.Domain.Enums;

namespace RetroMask.Application.Dtos.Voting;

public class VoteResultDto
{
    public Guid DiscussionPointId { get; set; }
    public int UpVotes { get; set; }
    public int DownVotes { get; set; }
    public int Score { get; set; }
    public VoteSummaryStatus Status { get; set; }
    public VoteType? MyVote { get; set; }
}

public class CastVoteRequest
{
    public VoteType VoteType { get; set; }
}
