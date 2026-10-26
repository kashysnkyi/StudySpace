using System.Collections.Concurrent;

namespace StudySpace.Web.Services;

public sealed record BoardPoint(double X, double Y);

public sealed record BoardStroke(string Color, double Width, IReadOnlyList<BoardPoint> Points);

public sealed class BoardStateService
{
    private const int MaximumStrokes = 2_000;
    private readonly ConcurrentDictionary<string, List<BoardStroke>> _boards = new();

    public IReadOnlyList<BoardStroke> GetStrokes(string boardId)
    {
        var strokes = _boards.GetOrAdd(Normalize(boardId), _ => []);
        lock (strokes)
        {
            return strokes.ToList();
        }
    }

    public void AddStroke(string boardId, BoardStroke stroke)
    {
        if (stroke.Points.Count is < 2 or > 5_000 || stroke.Width is < 1 or > 40)
        {
            return;
        }

        var strokes = _boards.GetOrAdd(Normalize(boardId), _ => []);
        lock (strokes)
        {
            strokes.Add(stroke);
            if (strokes.Count > MaximumStrokes)
            {
                strokes.RemoveRange(0, strokes.Count - MaximumStrokes);
            }
        }
    }

    public void Clear(string boardId) => _boards.TryRemove(Normalize(boardId), out _);

    private static string Normalize(string boardId) =>
        string.IsNullOrWhiteSpace(boardId) ? "main" : boardId.Trim().ToLowerInvariant();
}

