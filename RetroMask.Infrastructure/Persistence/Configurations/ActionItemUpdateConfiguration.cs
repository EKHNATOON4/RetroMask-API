using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.ActionItems;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class ActionItemUpdateConfiguration : IEntityTypeConfiguration<ActionItemUpdate>
{
    public void Configure(EntityTypeBuilder<ActionItemUpdate> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Note).HasMaxLength(1000).IsRequired();
        builder.HasOne(u => u.ActionItem).WithMany(a => a.Updates).HasForeignKey(u => u.ActionItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(u => u.Author).WithMany().HasForeignKey(u => u.AuthorId).OnDelete(DeleteBehavior.Restrict);
    }
}
