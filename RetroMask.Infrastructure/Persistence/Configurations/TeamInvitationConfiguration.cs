using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.Teams;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class TeamInvitationConfiguration : IEntityTypeConfiguration<TeamInvitation>
{
    public void Configure(EntityTypeBuilder<TeamInvitation> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.InvitedEmail).HasMaxLength(256).IsRequired();
        builder.Property(i => i.Token).HasMaxLength(256).IsRequired();
        builder.HasIndex(i => i.Token).IsUnique();
        builder.HasOne(i => i.Team).WithMany(t => t.Invitations).HasForeignKey(i => i.TeamId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.InvitedBy).WithMany().HasForeignKey(i => i.InvitedById).OnDelete(DeleteBehavior.Restrict);
    }
}
