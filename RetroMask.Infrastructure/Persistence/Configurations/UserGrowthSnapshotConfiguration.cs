using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.Insights;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class UserGrowthSnapshotConfiguration : IEntityTypeConfiguration<UserGrowthSnapshot>
{
    public void Configure(EntityTypeBuilder<UserGrowthSnapshot> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => new { s.UserId, s.Year, s.Month }).IsUnique();
        builder.Property(s => s.EngagementScore).HasPrecision(10, 4);
        builder.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
