using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.AI;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class AIInsightConfiguration : IEntityTypeConfiguration<AIInsight>
{
    public void Configure(EntityTypeBuilder<AIInsight> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Content).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(a => a.ModelUsed).HasMaxLength(100);
    }
}
