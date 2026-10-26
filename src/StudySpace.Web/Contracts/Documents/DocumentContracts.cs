using System.ComponentModel.DataAnnotations;
using StudySpace.Core.Entities;

namespace StudySpace.Web.Contracts.Documents;

public sealed record DocumentSummaryResponse(
    Guid Id,
    Guid WorkspaceId,
    string WorkspaceName,
    string Title,
    string Summary,
    string? CoverImageUrl,
    DocumentStatus Status,
    DateTime UpdatedAt);

public sealed record DocumentBlockResponse(Guid Id, BlockType Type, string Content, int Position);

public sealed record AttachmentResponse(
    Guid Id,
    string FileName,
    string StorageUrl,
    string ContentType,
    long SizeBytes);

public sealed record DocumentDetailsResponse(
    Guid Id,
    Guid WorkspaceId,
    string WorkspaceName,
    string Title,
    string Summary,
    string? CoverImageUrl,
    DocumentStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<DocumentBlockResponse> Blocks,
    IReadOnlyList<AttachmentResponse> Attachments);

public sealed class CreateDocumentRequest
{
    [Required]
    public Guid WorkspaceId { get; init; }

    [Required, StringLength(120, MinimumLength = 3)]
    public string Title { get; init; } = string.Empty;

    [StringLength(600)]
    public string Summary { get; init; } = string.Empty;

    [Url, StringLength(500)]
    public string? CoverImageUrl { get; init; }

    public DocumentStatus Status { get; init; } = DocumentStatus.Draft;
}

public sealed class UpdateDocumentRequest
{
    [Required]
    public Guid WorkspaceId { get; init; }

    [Required, StringLength(120, MinimumLength = 3)]
    public string Title { get; init; } = string.Empty;

    [StringLength(600)]
    public string Summary { get; init; } = string.Empty;

    [Url, StringLength(500)]
    public string? CoverImageUrl { get; init; }

    public DocumentStatus Status { get; init; }
}

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Skip,
    int Limit,
    int Total,
    string? NextLink);

