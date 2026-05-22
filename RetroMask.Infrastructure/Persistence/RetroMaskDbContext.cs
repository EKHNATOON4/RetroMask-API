using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Domain.Entities.Teams;
using RetroMask.Domain.Entities.Sessions;
using RetroMask.Domain.Entities.Discussion;
using RetroMask.Domain.Entities.Voting;
using RetroMask.Domain.Entities.Feedback;
using RetroMask.Domain.Entities.AI;
using RetroMask.Domain.Entities.ActionItems;
using RetroMask.Domain.Entities.Game;
using RetroMask.Domain.Entities.Insights;
using RetroMask.Domain.Entities.Files;
using RetroMask.Domain.Entities.Notifications;
using RetroMask.Domain.Common;
using System.Reflection;

namespace RetroMask.Infrastructure.Persistence;

public class RetroMaskDbContext : IdentityDbContext<ApplicationUser>
{
    public RetroMaskDbContext(DbContextOptions<RetroMaskDbContext> options) : base(options) { }

    // Identity
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Teams
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<TeamInvitation> TeamInvitations => Set<TeamInvitation>();

    // Sessions
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionMember> SessionMembers => Set<SessionMember>();
    public DbSet<SessionPhase> SessionPhases => Set<SessionPhase>();
    public DbSet<SessionTemplate> SessionTemplates => Set<SessionTemplate>();
    public DbSet<ModerationLog> ModerationLogs => Set<ModerationLog>();

    // Discussion
    public DbSet<DiscussionPoint> DiscussionPoints => Set<DiscussionPoint>();
    public DbSet<PointTag> PointTags => Set<PointTag>();
    public DbSet<PointReaction> PointReactions => Set<PointReaction>();
    public DbSet<DiscussionComment> DiscussionComments => Set<DiscussionComment>();

    // Voting
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<VoteSummary> VoteSummaries => Set<VoteSummary>();

    // Feedback
    public DbSet<FriendFeedback> FriendFeedbacks => Set<FriendFeedback>();
    public DbSet<FeedbackReaction> FeedbackReactions => Set<FeedbackReaction>();

    // AI
    public DbSet<AIInsight> AIInsights => Set<AIInsight>();
    public DbSet<AICluster> AIClusters => Set<AICluster>();
    public DbSet<AIReport> AIReports => Set<AIReport>();
    public DbSet<SentimentScore> SentimentScores => Set<SentimentScore>();

    // ActionItems
    public DbSet<ActionItem> ActionItems => Set<ActionItem>();
    public DbSet<ActionItemUpdate> ActionItemUpdates => Set<ActionItemUpdate>();

    // Game
    public DbSet<IcebreakerGame> IcebreakerGames => Set<IcebreakerGame>();
    public DbSet<GameResult> GameResults => Set<GameResult>();

    // Insights
    public DbSet<UserInsight> UserInsights => Set<UserInsight>();
    public DbSet<UserGrowthSnapshot> UserGrowthSnapshots => Set<UserGrowthSnapshot>();

    // Files & Notifications
    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
