using RetroMask.Domain.Common;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Domain.Enums;

namespace RetroMask.Domain.Entities.Files;

public class StoredFile : BaseEntity
{
    public string OriginalName { get; set; } = string.Empty;
    public string StoredName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public FileType FileType { get; set; }

    public string UploadedById { get; set; } = string.Empty;
    public ApplicationUser UploadedBy { get; set; } = null!;

    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }

    public string? PublicUrl { get; set; }
    public bool IsPublic { get; set; } = false;
}
