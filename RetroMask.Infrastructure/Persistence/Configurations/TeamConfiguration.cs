using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.Teams;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.InviteCode).HasMaxLength(20);
        builder.HasIndex(t => t.InviteCode).IsUnique().HasFilter("[InviteCode] IS NOT NULL");
        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
