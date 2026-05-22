using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.Teams;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => new { m.TeamId, m.UserId }).IsUnique();
        builder.HasOne(m => m.Team).WithMany(t => t.Members).HasForeignKey(m => m.TeamId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.User).WithMany(u => u.TeamMemberships).HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
