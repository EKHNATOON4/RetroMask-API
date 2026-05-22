using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.Voting;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class VoteConfiguration : IEntityTypeConfiguration<Vote>
{
    public void Configure(EntityTypeBuilder<Vote> builder)
    {
        builder.HasKey(v => v.Id);
        builder.HasIndex(v => new { v.DiscussionPointId, v.UserId }).IsUnique();
        builder.HasOne(v => v.DiscussionPoint).WithMany(p => p.Votes).HasForeignKey(v => v.DiscussionPointId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(v => v.User).WithMany().HasForeignKey(v => v.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
