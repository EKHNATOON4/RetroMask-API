using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.Files;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.OriginalName).HasMaxLength(256).IsRequired();
        builder.Property(f => f.StoredName).HasMaxLength(256).IsRequired();
        builder.Property(f => f.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(f => f.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(f => f.PublicUrl).HasMaxLength(500);
        builder.HasOne(f => f.UploadedBy).WithMany().HasForeignKey(f => f.UploadedById).OnDelete(DeleteBehavior.Restrict);
    }
}
