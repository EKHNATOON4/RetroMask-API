using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.Game;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class IcebreakerGameConfiguration : IEntityTypeConfiguration<IcebreakerGame>
{
    public void Configure(EntityTypeBuilder<IcebreakerGame> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.GameType).HasMaxLength(100).IsRequired();
        builder.Property(g => g.Title).HasMaxLength(200).IsRequired();
        builder.Property(g => g.ConfigJson).HasColumnType("nvarchar(max)");
        builder.HasOne(g => g.Session).WithMany().HasForeignKey(g => g.SessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
