using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.Sessions;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class SessionPhaseConfiguration : IEntityTypeConfiguration<SessionPhase>
{
    public void Configure(EntityTypeBuilder<SessionPhase> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.HasOne(p => p.Session).WithMany(s => s.Phases).HasForeignKey(p => p.SessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
