using Microsoft.EntityFrameworkCore;
using TimeTrek.Infrastructure.Persistence;

namespace TimeTrek.Infrastructure.Tests.Persistence;

internal sealed class SqliteTestDatabase : IDbContextFactory<TimeTrekDbContext>, IAsyncDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "TimeTrek.Tests",
        Guid.CreateVersion7().ToString("N"));

    private readonly DbContextOptions<TimeTrekDbContext> options;

    public string DirectoryPath => directory;

    public SqliteTestDatabase()
    {
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "test.db");
        options = new DbContextOptionsBuilder<TimeTrekDbContext>()
            .UseSqlite($"Data Source={path};Pooling=False")
            .Options;
    }

    public TimeTrekDbContext CreateDbContext() => new(options);

    public Task<TimeTrekDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateDbContext());

    public async Task InitializeAsync()
    {
        await using TimeTrekDbContext context = CreateDbContext();
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
