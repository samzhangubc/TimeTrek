using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TimeTrek.Infrastructure.Persistence;

public sealed class DatabaseLifecycleService(
    IDbContextFactory<TimeTrekDbContext> contextFactory,
    TimeProvider timeProvider,
    ILogger<DatabaseLifecycleService> logger) : IHostedService
{
    private static readonly Action<ILogger, Exception?> LogUncleanShutdown = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(2000, nameof(LogUncleanShutdown)),
        "An unclean shutdown was detected; SQLite integrity validation completed before writes resumed.");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using TimeTrekDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON;", cancellationToken).ConfigureAwait(false);
        await context.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;", cancellationToken).ConfigureAwait(false);
        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken).ConfigureAwait(false);
        await context.Database.ExecuteSqlRawAsync("PRAGMA synchronous=FULL;", cancellationToken).ConfigureAwait(false);
        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        SchemaStateRow? state = await context.SchemaStates.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (state is null)
        {
            state = new SchemaStateRow
            {
                SchemaVersion = 1,
                CleanShutdown = false,
                UpdatedUtcMilliseconds = timeProvider.GetUtcNow().ToUnixTimeMilliseconds(),
            };
            context.SchemaStates.Add(state);
        }
        else
        {
            if (!state.CleanShutdown)
            {
                await ValidateIntegrityAsync(context, cancellationToken).ConfigureAwait(false);
                LogUncleanShutdown(logger, null);
            }

            state.CleanShutdown = false;
            state.UpdatedUtcMilliseconds = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await using TimeTrekDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        SchemaStateRow? state = await context.SchemaStates.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (state is null)
        {
            return;
        }

        state.CleanShutdown = true;
        state.ConsecutiveStartupFailures = 0;
        state.UpdatedUtcMilliseconds = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task ValidateIntegrityAsync(TimeTrekDbContext context, CancellationToken cancellationToken)
    {
        await using System.Data.Common.DbCommand command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "PRAGMA quick_check;";
        object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (!string.Equals(result?.ToString(), "ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The TimeTrek database failed SQLite integrity validation and was preserved without replacement.");
        }
    }
}
