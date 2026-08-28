using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ThymeMe.Infrastructure.Persistence;

public sealed class ThymeMeDesignTimeDbContextFactory : IDesignTimeDbContextFactory<ThymeMeDbContext>
{
    public ThymeMeDbContext CreateDbContext(string[] args)
    {
        string path = Path.Combine(Path.GetTempPath(), "thymeme.design.db");
        DbContextOptionsBuilder<ThymeMeDbContext> builder = new();
        builder.UseSqlite($"Data Source={path}");
        return new ThymeMeDbContext(builder.Options);
    }
}
