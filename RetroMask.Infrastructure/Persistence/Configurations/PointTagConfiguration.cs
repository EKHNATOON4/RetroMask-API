using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.Discussion;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class PointTagConfiguration : IEntityTypeConfiguration<PointTag>
{
    public void Configure(EntityTypeBuilder<PointTag> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Label).HasMaxLength(50).IsRequired();
        builder.Property(t => t.ColorHex).HasMaxLength(7);
        builder.HasOne(t => t.DiscussionPoint).WithMany(p => p.Tags).HasForeignKey(t => t.DiscussionPointId).OnDelete(DeleteBehavior.Cascade);
    }
}
