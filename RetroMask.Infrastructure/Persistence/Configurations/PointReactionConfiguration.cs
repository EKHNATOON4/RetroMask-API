using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.Discussion;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class PointReactionConfiguration : IEntityTypeConfiguration<PointReaction>
{
    public void Configure(EntityTypeBuilder<PointReaction> builder)
    {
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => new { r.DiscussionPointId, r.UserId, r.ReactionType }).IsUnique();
        builder.HasOne(r => r.DiscussionPoint).WithMany(p => p.Reactions).HasForeignKey(r => r.DiscussionPointId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
