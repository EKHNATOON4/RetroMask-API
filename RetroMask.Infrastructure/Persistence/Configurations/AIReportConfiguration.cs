using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.AI;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class AIReportConfiguration : IEntityTypeConfiguration<AIReport>
{
    public void Configure(EntityTypeBuilder<AIReport> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Title).HasMaxLength(300).IsRequired();
        builder.Property(r => r.MarkdownContent).HasColumnType("nvarchar(max)");
        builder.Property(r => r.HtmlContent).HasColumnType("nvarchar(max)");
        builder.Property(r => r.ModelUsed).HasMaxLength(100);
        builder.HasOne(r => r.Session).WithMany(s => s.AIReports).HasForeignKey(r => r.SessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
