using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.Sessions;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class SessionMemberConfiguration : IEntityTypeConfiguration<SessionMember>
{
    public void Configure(EntityTypeBuilder<SessionMember> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.MaskName).HasMaxLength(100);
        builder.HasOne(m => m.Session).WithMany(s => s.Members).HasForeignKey(m => m.SessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
