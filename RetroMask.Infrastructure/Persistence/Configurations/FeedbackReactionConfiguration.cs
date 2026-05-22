using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.Feedback;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class FeedbackReactionConfiguration : IEntityTypeConfiguration<FeedbackReaction>
{
    public void Configure(EntityTypeBuilder<FeedbackReaction> builder)
    {
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => new { r.FriendFeedbackId, r.UserId, r.ReactionType }).IsUnique();
        builder.HasOne(r => r.FriendFeedback).WithMany(f => f.Reactions).HasForeignKey(r => r.FriendFeedbackId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
