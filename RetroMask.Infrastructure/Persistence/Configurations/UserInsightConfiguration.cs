using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.Insights;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class UserInsightConfiguration : IEntityTypeConfiguration<UserInsight>
{
    public void Configure(EntityTypeBuilder<UserInsight> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.InsightJson).HasColumnType("nvarchar(max)");
        builder.HasOne(i => i.User).WithMany(u => u.Insights).HasForeignKey(i => i.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
