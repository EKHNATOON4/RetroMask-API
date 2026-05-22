using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.Discussion;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class DiscussionCommentConfiguration : IEntityTypeConfiguration<DiscussionComment>
{
    public void Configure(EntityTypeBuilder<DiscussionComment> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Content).HasMaxLength(1000).IsRequired();
        builder.HasQueryFilter(c => !c.IsDeleted);
        builder.HasOne(c => c.DiscussionPoint).WithMany(p => p.Comments).HasForeignKey(c => c.DiscussionPointId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(c => c.Author).WithMany().HasForeignKey(c => c.AuthorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.ParentComment).WithMany(pc => pc.Replies).HasForeignKey(c => c.ParentCommentId).OnDelete(DeleteBehavior.Restrict);
    }
}
