namespace StudySpace.Web.Models;

public sealed record InsightsViewModel(
    int WorkspaceCount,
    int DocumentCount,
    int BlockCount,
    int AttachmentCount,
    IReadOnlyDictionary<string, int> DocumentsByStatus);

