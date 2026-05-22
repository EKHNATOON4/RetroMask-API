using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.Sessions;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class SessionTemplateConfiguration : IEntityTypeConfiguration<SessionTemplate>
{
    public void Configure(EntityTypeBuilder<SessionTemplate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.PhasesJson).HasColumnType("nvarchar(max)");
    }
}
