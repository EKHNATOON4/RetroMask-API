using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.Discussion;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class DiscussionPointConfiguration : IEntityTypeConfiguration<DiscussionPoint>
{
    public void Configure(EntityTypeBuilder<DiscussionPoint> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Content).HasMaxLength(2000).IsRequired();
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasOne(p => p.Phase).WithMany(ph => ph.DiscussionPoints).HasForeignKey(p => p.PhaseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.Author).WithMany().HasForeignKey(p => p.AuthorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.VoteSummary).WithOne(vs => vs.DiscussionPoint).HasForeignKey<Domain.Entities.Voting.VoteSummary>(vs => vs.DiscussionPointId);
    }
}
