using Microsoft.EntityFrameworkCore;
using ThymeMe.Infrastructure.Persistence;

namespace ThymeMe.Infrastructure.Tests.Persistence;

internal sealed class SqliteTestDatabase : IDbContextFactory<ThymeMeDbContext>, IAsyncDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "ThymeMe.Tests",
        Guid.CreateVersion7().ToString("N"));

    private readonly DbContextOptions<ThymeMeDbContext> options;

    public string DirectoryPath => directory;

    public SqliteTestDatabase()
    {
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "test.db");
        options = new DbContextOptionsBuilder<ThymeMeDbContext>()
            .UseSqlite($"Data Source={path};Pooling=False")
            .Options;
    }

    public ThymeMeDbContext CreateDbContext() => new(options);

    public Task<ThymeMeDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateDbContext());

    public async Task InitializeAsync()
    {
        await using ThymeMeDbContext context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }

        return ValueTask.CompletedTask;
    }
}

internal sealed class MutableTimeProvider(DateTimeOffset initial) : TimeProvider
{
    private DateTimeOffset utcNow = initial;

    public override DateTimeOffset GetUtcNow() => utcNow;

    public void Advance(TimeSpan duration) => utcNow += duration;
}
