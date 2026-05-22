using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.Game;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class GameResultConfiguration : IEntityTypeConfiguration<GameResult>
{
    public void Configure(EntityTypeBuilder<GameResult> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Answer).HasMaxLength(1000);
        builder.Property(r => r.MetadataJson).HasColumnType("nvarchar(max)");
        builder.HasOne(r => r.IcebreakerGame).WithMany(g => g.Results).HasForeignKey(r => r.IcebreakerGameId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
