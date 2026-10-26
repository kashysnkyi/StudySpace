using StudySpace.Core.Entities;

namespace StudySpace.Web.Models;

public sealed record DashboardViewModel(
    int WorkspaceCount,
    int DocumentCount,
    int PublishedCount,
    int AttachmentCount,
    IReadOnlyList<Document> RecentDocuments);

