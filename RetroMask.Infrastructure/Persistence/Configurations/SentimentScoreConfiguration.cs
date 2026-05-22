using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.AI;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class SentimentScoreConfiguration : IEntityTypeConfiguration<SentimentScore>
{
    public void Configure(EntityTypeBuilder<SentimentScore> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ModelUsed).HasMaxLength(100);
        builder.Property(s => s.Score).HasPrecision(5, 4);
        builder.Property(s => s.PositiveScore).HasPrecision(5, 4);
        builder.Property(s => s.NeutralScore).HasPrecision(5, 4);
        builder.Property(s => s.NegativeScore).HasPrecision(5, 4);
    }
}
