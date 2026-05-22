using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.Sessions;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Title).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(1000);
        builder.HasQueryFilter(s => !s.IsDeleted);
        builder.HasOne(s => s.Team).WithMany(t => t.Sessions).HasForeignKey(s => s.TeamId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.Facilitator).WithMany().HasForeignKey(s => s.FacilitatorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Template).WithMany(t => t.Sessions).HasForeignKey(s => s.TemplateId).OnDelete(DeleteBehavior.SetNull);
    }
}
