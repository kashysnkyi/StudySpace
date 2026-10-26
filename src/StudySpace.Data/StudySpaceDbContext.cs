using Microsoft.EntityFrameworkCore;
using StudySpace.Core.Entities;

namespace StudySpace.Data;

public sealed class StudySpaceDbContext(DbContextOptions<StudySpaceDbContext> options)
    : DbContext(options)
{
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentBlock> DocumentBlocks => Set<DocumentBlock>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Workspace>()
            .HasMany(workspace => workspace.Documents)
            .WithOne(document => document.Workspace)
            .HasForeignKey(document => document.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Workspace>()
            .HasMany(workspace => workspace.Members)
            .WithOne(member => member.Workspace)
            .HasForeignKey(member => member.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkspaceMember>()
            .HasIndex(member => new { member.WorkspaceId, member.Subject })
            .IsUnique();

        modelBuilder.Entity<Document>()
            .HasMany(document => document.Blocks)
            .WithOne(block => block.Document)
            .HasForeignKey(block => block.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Document>()
            .HasMany(document => document.Attachments)
            .WithOne(attachment => attachment.Document)
            .HasForeignKey(attachment => attachment.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Document>()
            .HasIndex(document => new { document.WorkspaceId, document.Title });

        modelBuilder.Entity<DocumentBlock>()
            .HasIndex(block => new { block.DocumentId, block.Position })
            .IsUnique();
    }
}

