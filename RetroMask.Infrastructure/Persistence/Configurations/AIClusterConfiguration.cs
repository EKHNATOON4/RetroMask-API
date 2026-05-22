using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.AI;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class AIClusterConfiguration : IEntityTypeConfiguration<AICluster>
{
    public void Configure(EntityTypeBuilder<AICluster> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Label).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Summary).HasMaxLength(1000);
        builder.Property(c => c.ColorHex).HasMaxLength(7);
        builder.Property(c => c.PointIdsJson).HasColumnType("nvarchar(max)");
    }
}
