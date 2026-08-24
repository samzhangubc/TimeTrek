using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TimeTrek.Infrastructure.Persistence;

public sealed class TimeTrekDesignTimeDbContextFactory : IDesignTimeDbContextFactory<TimeTrekDbContext>
{
    public TimeTrekDbContext CreateDbContext(string[] args)
    {
        string path = Path.Combine(Path.GetTempPath(), "TimeTrek.Design.db");
        DbContextOptionsBuilder<TimeTrekDbContext> builder = new();
        builder.UseSqlite($"Data Source={path}");
        return new TimeTrekDbContext(builder.Options);
    }
}
