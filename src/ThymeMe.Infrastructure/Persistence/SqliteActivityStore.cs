using Microsoft.EntityFrameworkCore;
using ThymeMe.Application.ActivityTracking;

namespace ThymeMe.Infrastructure.Persistence;

public sealed class SqliteActivityStore(IDbContextFactory<ThymeMeDbContext> contextFactory) : IActivityStore
{
    public async ValueTask AddDurationAsync(
        Guid sessionId,
        string displayName,
        string executableFileName,
        long durationMilliseconds,
        CancellationToken cancellationToken = default)
    {
        if (durationMilliseconds <= 0 || displayName.Length is 0 or > 200 || executableFileName.Length is 0 or > 260)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMilliseconds));
        }

        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        ForegroundApplicationRow? row = await context.ForegroundApplications.SingleOrDefaultAsync(
            item => item.SessionId == sessionId && item.ExecutableFileName == executableFileName,
            cancellationToken).ConfigureAwait(false);
        if (row is null)
        {
            context.ForegroundApplications.Add(new ForegroundApplicationRow
            {
                Id = Guid.CreateVersion7(),
                SessionId = sessionId,
                DisplayName = displayName,
                ExecutableFileName = executableFileName,
                DurationMilliseconds = durationMilliseconds,
            });
        }
        else
        {
            row.DurationMilliseconds = checked(row.DurationMilliseconds + durationMilliseconds);
            row.DisplayName = displayName;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<IReadOnlyList<ApplicationDuration>> GetForSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await context.ForegroundApplications.AsNoTracking().Where(row => row.SessionId == sessionId)
            .OrderByDescending(row => row.DurationMilliseconds)
            .Select(row => new ApplicationDuration(row.DisplayName, row.ExecutableFileName, row.DurationMilliseconds))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask PurgeAllAsync(CancellationToken cancellationToken = default)
    {
        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await context.ForegroundApplications.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
