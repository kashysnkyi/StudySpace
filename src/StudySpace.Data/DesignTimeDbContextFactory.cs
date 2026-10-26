using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StudySpace.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<StudySpaceDbContext>
{
    public StudySpaceDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<StudySpaceDbContext>()
            .UseSqlite("Data Source=studyspace.design.db")
            .Options;

        return new StudySpaceDbContext(options);
    }
}
