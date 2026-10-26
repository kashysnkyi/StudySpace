using System.ComponentModel.DataAnnotations;

namespace StudySpace.Core.Entities;

public sealed class Attachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }

    [Required, StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string StorageUrl { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string ContentType { get; set; } = "application/octet-stream";

    [Range(0, 10 * 1024 * 1024)]
    public long SizeBytes { get; set; }
}

