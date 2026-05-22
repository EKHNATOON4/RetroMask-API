using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.Feedback;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class FriendFeedbackConfiguration : IEntityTypeConfiguration<FriendFeedback>
{
    public void Configure(EntityTypeBuilder<FriendFeedback> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Content).HasMaxLength(2000).IsRequired();
        builder.HasQueryFilter(f => !f.IsDeleted);
        builder.HasOne(f => f.Giver).WithMany().HasForeignKey(f => f.GiverId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(f => f.Receiver).WithMany().HasForeignKey(f => f.ReceiverId).OnDelete(DeleteBehavior.Restrict);
    }
}
