using System.ComponentModel.DataAnnotations;
using StudySpace.Core.Entities;

namespace StudySpace.Web.Contracts.Blocks;

public sealed record BlockResponse(Guid Id, BlockType Type, string Content, int Position);

public sealed class CreateBlockRequest
{
    public BlockType Type { get; init; } = BlockType.Paragraph;

    [Required, StringLength(10_000)]
    public string Content { get; init; } = string.Empty;
}

public sealed class UpdateBlockRequest
{
    public BlockType Type { get; init; }

    [Required, StringLength(10_000)]
    public string Content { get; init; } = string.Empty;
}

public sealed class ReorderBlocksRequest
{
    [Required, MinLength(1)]
    public IReadOnlyList<Guid> BlockIds { get; init; } = [];
}

