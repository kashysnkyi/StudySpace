using Microsoft.EntityFrameworkCore;
using StudySpace.Core.Entities;

namespace StudySpace.Data;

public static class DatabaseSeeder
{
    private static readonly SemaphoreSlim SeedLock = new(1, 1);
    private static readonly Guid DemoWorkspaceId = Guid.Parse("e08c2747-1c44-4fde-920f-7f3598e852cf");
    private static readonly Guid WelcomeDocumentId = Guid.Parse("5ded85a3-035d-46af-934e-5c1c204a4ec7");

    public static async Task SeedAsync(StudySpaceDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await SeedLock.WaitAsync(cancellationToken);
        try
        {
            await SeedCoreAsync(dbContext, cancellationToken);
        }
        finally
        {
            SeedLock.Release();
        }
    }

    private static async Task SeedCoreAsync(
        StudySpaceDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        if (await dbContext.Workspaces.AnyAsync(workspace => workspace.Id == DemoWorkspaceId, cancellationToken))
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Workspaces SET Name = {"Веб-технології"}, Description = {"Спільне місце для нотаток, планів і матеріалів проєкту."}, Plan = {(int)SubscriptionPlan.Premium} WHERE Id = {DemoWorkspaceId}",
                cancellationToken);
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Documents SET Title = {"Ласкаво просимо до StudySpace"}, Summary = {"Документ демонструє перший етап MVC та блочну модель вмісту."} WHERE Id = {WelcomeDocumentId}",
                cancellationToken);
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE DocumentBlocks SET Content = CASE Position WHEN 0 THEN {"Створюймо знання разом"} WHEN 1 THEN {"StudySpace зберігає документи, обговорення, файли та матеріали проєкту в одному спокійному робочому просторі."} WHEN 2 THEN {"Створити основу MVC\nДодати REST API\nУвімкнути співпрацю в реальному часі"} ELSE Content END WHERE DocumentId = {WelcomeDocumentId}",
                cancellationToken);
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT OR IGNORE INTO WorkspaceMembers (Id, WorkspaceId, Subject, DisplayName, Role) VALUES ({Guid.NewGuid()}, {DemoWorkspaceId}, {"demo-user"}, {"Демонстраційний студент"}, {(int)WorkspaceRole.Owner})",
                cancellationToken);
            return;
        }

        if (await dbContext.Workspaces.AnyAsync(cancellationToken))
        {
            return;
        }

        var workspace = new Workspace
        {
            Id = DemoWorkspaceId,
            Name = "Веб-технології",
            Description = "Спільне місце для нотаток, планів і матеріалів проєкту.",
            Plan = SubscriptionPlan.Premium,
            Latitude = 50.4501,
            Longitude = 30.5234,
            Members =
            {
                new WorkspaceMember
                {
                    Subject = "demo-user",
                    DisplayName = "Демонстраційний студент",
                    Role = WorkspaceRole.Owner
                }
            }
        };

        workspace.Documents.Add(new Document
        {
            Id = WelcomeDocumentId,
            Title = "Ласкаво просимо до StudySpace",
            Summary = "Документ демонструє перший етап MVC та блочну модель вмісту.",
            Status = DocumentStatus.Published,
            CoverImageUrl = "https://images.unsplash.com/photo-1455390582262-044cdead277a?auto=format&fit=crop&w=1200&q=80",
            Blocks =
            {
                new DocumentBlock
                {
                    Type = BlockType.Heading,
                    Position = 0,
                    Content = "Створюймо знання разом"
                },
                new DocumentBlock
                {
                    Type = BlockType.Paragraph,
                    Position = 1,
                    Content = "StudySpace зберігає документи, обговорення, файли та матеріали проєкту в одному спокійному робочому просторі."
                },
                new DocumentBlock
                {
                    Type = BlockType.Checklist,
                    Position = 2,
                    Content = "Створити основу MVC\nДодати REST API\nУвімкнути співпрацю в реальному часі"
                }
            }
        });

        dbContext.Workspaces.Add(workspace);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

