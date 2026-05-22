using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.ActionItems;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class ActionItemConfiguration : IEntityTypeConfiguration<ActionItem>
{
    public void Configure(EntityTypeBuilder<ActionItem> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Title).HasMaxLength(300).IsRequired();
        builder.Property(a => a.Description).HasMaxLength(2000);
        builder.HasQueryFilter(a => !a.IsDeleted);
        builder.HasOne(a => a.Session).WithMany().HasForeignKey(a => a.SessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.AssignedTo).WithMany().HasForeignKey(a => a.AssignedToId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.CreatedBy).WithMany().HasForeignKey(a => a.CreatedById).OnDelete(DeleteBehavior.Restrict);
    }
}
