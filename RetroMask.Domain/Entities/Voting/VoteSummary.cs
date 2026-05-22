using RetroMask.Domain.Common;
using RetroMask.Domain.Entities.Discussion;
using RetroMask.Domain.Enums;

namespace RetroMask.Domain.Entities.Voting;

public class VoteSummary : BaseEntity
{
    public Guid DiscussionPointId { get; set; }
    public DiscussionPoint DiscussionPoint { get; set; } = null!;

    public int UpVotes { get; set; } = 0;
    public int DownVotes { get; set; } = 0;
    public int TotalVotes => UpVotes + DownVotes;
    public int Score => UpVotes - DownVotes;

    public VoteSummaryStatus Status { get; set; } = VoteSummaryStatus.Open;
    public DateTime? ClosedAt { get; set; }
}
