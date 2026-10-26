using System.ComponentModel.DataAnnotations;

namespace StudySpace.Core.Entities;

public sealed class DocumentBlock
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }

    public BlockType Type { get; set; } = BlockType.Paragraph;

    [Required, StringLength(10_000)]
    public string Content { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Position { get; set; }
}

public enum BlockType
{
    Paragraph,
    Heading,
    Checklist,
    Code,
    Quote
}

