using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.Sessions;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class ModerationLogConfiguration : IEntityTypeConfiguration<ModerationLog>
{
    public void Configure(EntityTypeBuilder<ModerationLog> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Action).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Reason).HasMaxLength(500);
        builder.HasOne(l => l.Session).WithMany(s => s.ModerationLogs).HasForeignKey(l => l.SessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(l => l.Moderator).WithMany().HasForeignKey(l => l.ModeratorId).OnDelete(DeleteBehavior.Restrict);
    }
}
