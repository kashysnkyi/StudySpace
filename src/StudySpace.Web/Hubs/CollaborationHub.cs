using Microsoft.AspNetCore.SignalR;

namespace StudySpace.Web.Hubs;

public sealed class CollaborationHub(StudySpace.Web.Services.BoardStateService boardState) : Hub
{
    public Task JoinDocument(Guid documentId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupName(documentId));

    public Task LeaveDocument(Guid documentId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(documentId));

    public Task CursorChanged(Guid documentId, Guid blockId, string collaborator) =>
        Clients.OthersInGroup(GroupName(documentId))
            .SendAsync("cursorChanged", new { blockId, collaborator });

    public async Task<IReadOnlyList<StudySpace.Web.Services.BoardStroke>> JoinBoard(string boardId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, BoardGroupName(boardId));
        return boardState.GetStrokes(boardId);
    }

    public async Task DrawStroke(string boardId, StudySpace.Web.Services.BoardStroke stroke)
    {
        boardState.AddStroke(boardId, stroke);
        await Clients.OthersInGroup(BoardGroupName(boardId)).SendAsync("strokeDrawn", stroke);
    }

    public async Task ClearBoard(string boardId)
    {
        boardState.Clear(boardId);
        await Clients.OthersInGroup(BoardGroupName(boardId)).SendAsync("boardCleared");
    }

    public static string GroupName(Guid documentId) => $"document:{documentId:N}";
    public static string BoardGroupName(string boardId) => $"board:{boardId.Trim().ToLowerInvariant()}";
}

