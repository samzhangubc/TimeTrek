using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TimeTrek.Application.Timing;
using TimeTrek.Domain.Billing;
using TimeTrek.Domain.Organization;
using TimeTrek.Domain.Reporting;
using TimeTrek.Domain.Timing;

namespace TimeTrek.Infrastructure.Persistence;

public sealed class SqliteTimingStore(IDbContextFactory<TimeTrekDbContext> contextFactory) : ITimingStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { MaxDepth = 4 };

    public async ValueTask<TimingSnapshot?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        await using TimeTrekDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        TimingRootRow? row = await context.TimingRoots.AsNoTracking().SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return row is null ? null : ToDomain(row);
    }

    public async ValueTask<bool> TryStartAsync(TimingSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await using TimeTrekDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);

        if (await context.TimingRoots.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        context.TimingRoots.Add(ToRow(snapshot));
        context.Sessions.Add(new SessionRow
        {
            Id = snapshot.SessionId,
            Origin = (int)SessionOrigin.Tracked,
            TimingMode = (int)snapshot.Mode,
            IsDraft = true,
            StreamId = snapshot.Associations.StreamId,
            ProjectId = snapshot.Associations.ProjectId,
            StartUtcMilliseconds = snapshot.StartedUtcMilliseconds,
            EndUtcMilliseconds = snapshot.StartedUtcMilliseconds,
            TimeZoneId = string.Empty,
            RequestedDurationMilliseconds = snapshot.RequestedDurationMilliseconds,
            IsBillable = snapshot.Billing.IsBillable,
            HourlyRateMinorUnits = snapshot.Billing.HourlyRateMinorUnits,
            CurrencyCode = snapshot.Billing.CurrencyCode,
            CreatedUtcMilliseconds = snapshot.StartedUtcMilliseconds,
            UpdatedUtcMilliseconds = snapshot.StartedUtcMilliseconds,
            Categories = snapshot.Associations.CategoryIds.Select(categoryId => new SessionCategoryRow
            {
                SessionId = snapshot.SessionId,
                CategoryId = categoryId,
            }).ToList(),
        });

        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public async ValueTask<bool> TryTransitionAsync(
        TimingSnapshot snapshot,
        int expectedRevision,
        CancellationToken cancellationToken = default)
    {
        await using TimeTrekDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        TimingRootRow? row = await context.TimingRoots.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (row is null || row.Revision != expectedRevision || row.Id != snapshot.RootId)
        {
            return false;
        }

        TimingSnapshot previous = ToDomain(row);
        AppendCompletedSegment(context, previous, snapshot.CurrentSegmentStartedUtcMilliseconds);
        Apply(row, snapshot);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async ValueTask<bool> TryFinalizeAsync(
        TimingSnapshot snapshot,
        int expectedRevision,
        CompletedSession session,
        CancellationToken cancellationToken = default)
    {
        await using TimeTrekDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        TimingRootRow? root = await context.TimingRoots.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        SessionRow? row = await context.Sessions.Include(item => item.Categories)
            .SingleOrDefaultAsync(item => item.Id == session.Id, cancellationToken).ConfigureAwait(false);
        if (root is null || row is null || root.Revision != expectedRevision || root.Id != snapshot.RootId)
        {
            return false;
        }

        AppendCompletedSegment(context, ToDomain(root), session.EndUtcMilliseconds);
        row.IsDraft = false;
        row.Origin = (int)session.Origin;
        row.TimingMode = (int)session.Mode;
        row.StartUtcMilliseconds = session.StartUtcMilliseconds;
        row.EndUtcMilliseconds = session.EndUtcMilliseconds;
        row.TimeZoneId = session.TimeZoneId;
        row.StartUtcOffsetMinutes = session.StartUtcOffsetMinutes;
        row.EndUtcOffsetMinutes = session.EndUtcOffsetMinutes;
        row.RawDurationMilliseconds = session.Duration.RawMilliseconds;
        row.EffectiveDurationMilliseconds = session.Duration.EffectiveMilliseconds;
        row.RoundingIncrementMinutes = session.Duration.IncrementMinutes;
        row.RoundingRule = (int)session.Duration.Rule;
        row.RequestedDurationMilliseconds = session.RequestedDurationMilliseconds;
        row.Description = session.Description;
        row.CompletionPending = session.CompletionPending;
        row.IsBillable = session.Billing.IsBillable;
        row.HourlyRateMinorUnits = session.Billing.HourlyRateMinorUnits;
        row.CurrencyCode = session.Billing.CurrencyCode;
        row.EstimatedEarningMinorUnits = session.Billing.EstimatedEarningMinorUnits;
        row.UpdatedUtcMilliseconds = session.UpdatedUtcMilliseconds;
        context.TimingRoots.Remove(root);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async ValueTask CompleteDescriptionAsync(
        Guid sessionId,
        string? description,
        long updatedUtcMilliseconds,
        CancellationToken cancellationToken = default)
    {
        await using TimeTrekDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        int changed = await context.Sessions
            .Where(row => row.Id == sessionId && row.CompletionPending && !row.IsDeleted && !row.IsDraft)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(row => row.Description, description)
                .SetProperty(row => row.CompletionPending, false)
                .SetProperty(row => row.UpdatedUtcMilliseconds, updatedUtcMilliseconds), cancellationToken)
            .ConfigureAwait(false);
        if (changed != 1)
        {
            throw new InvalidOperationException("The pending completion no longer exists.");
        }
    }

    public async ValueTask<IReadOnlyList<CompletedSession>> ListPendingCompletionsAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        int boundedLimit = Math.Clamp(limit, 1, 200);
        await using TimeTrekDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        List<SessionRow> rows = await context.Sessions.AsNoTracking().Include(row => row.Categories)
            .Where(row => row.CompletionPending && !row.IsDeleted && !row.IsDraft)
            .OrderBy(row => row.EndUtcMilliseconds)
            .Take(boundedLimit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(ToDomain).ToList();
    }

    private static void AppendCompletedSegment(TimeTrekDbContext context, TimingSnapshot previous, long endUtcMilliseconds)
    {
        if (endUtcMilliseconds <= previous.CurrentSegmentStartedUtcMilliseconds)
        {
            return;
        }

        TimingSegmentKind kind = previous.Status switch
        {
            TimingStatus.ActiveWork => TimingSegmentKind.Work,
            TimingStatus.BreakPending => TimingSegmentKind.Buffer,
            TimingStatus.ActiveBreak => TimingSegmentKind.Break,
            TimingStatus.Paused or TimingStatus.WorkStartPending => TimingSegmentKind.Pause,
            _ => TimingSegmentKind.Pause,
        };
        context.TimingSegments.Add(new TimingSegmentRow
        {
            Id = Guid.CreateVersion7(),
            SessionId = previous.SessionId,
            Kind = (int)kind,
            StartUtcMilliseconds = previous.CurrentSegmentStartedUtcMilliseconds,
            EndUtcMilliseconds = endUtcMilliseconds,
        });
    }

    private static TimingRootRow ToRow(TimingSnapshot snapshot)
    {
        TimingRootRow row = new()
        {
            Id = snapshot.RootId,
            CategoryIdsJson = string.Empty,
        };
        Apply(row, snapshot);
        return row;
    }

    private static void Apply(TimingRootRow row, TimingSnapshot snapshot)
    {
        row.SessionId = snapshot.SessionId;
        row.Status = (int)snapshot.Status;
        row.TimingMode = (int)snapshot.Mode;
        row.StreamId = snapshot.Associations.StreamId;
        row.ProjectId = snapshot.Associations.ProjectId;
        row.CategoryIdsJson = JsonSerializer.Serialize(snapshot.Associations.CategoryIds.Order(), JsonOptions);
        row.IsBillable = snapshot.Billing.IsBillable;
        row.HourlyRateMinorUnits = snapshot.Billing.HourlyRateMinorUnits;
        row.CurrencyCode = snapshot.Billing.CurrencyCode;
        row.RoundingIncrementMinutes = snapshot.RoundingIncrementMinutes;
        row.RoundingRule = (int)snapshot.RoundingRule;
        row.StartedUtcMilliseconds = snapshot.StartedUtcMilliseconds;
        row.CurrentSegmentStartedUtcMilliseconds = snapshot.CurrentSegmentStartedUtcMilliseconds;
        row.RequestedDurationMilliseconds = snapshot.RequestedDurationMilliseconds;
        row.ScheduledEndUtcMilliseconds = snapshot.ScheduledEndUtcMilliseconds;
        row.AccumulatedWorkMilliseconds = snapshot.AccumulatedWorkMilliseconds;
        row.AccumulatedBreakMilliseconds = snapshot.AccumulatedBreakMilliseconds;
        row.PomodoroTotalMilliseconds = snapshot.Pomodoro?.TotalMilliseconds;
        row.PomodoroWorkMilliseconds = snapshot.Pomodoro?.WorkMilliseconds;
        row.PomodoroBreakMilliseconds = snapshot.Pomodoro?.BreakMilliseconds;
        row.PomodoroBufferMilliseconds = snapshot.Pomodoro?.BufferMilliseconds;
        row.PomodoroBreaksBillable = snapshot.Pomodoro?.BreaksBillable ?? false;
        row.PausedFromStatus = snapshot.PausedFromStatus is TimingStatus status ? (int)status : null;
        row.Revision = snapshot.Revision;
    }

    private static TimingSnapshot ToDomain(TimingRootRow row)
    {
        Guid[] categoryIds = JsonSerializer.Deserialize<Guid[]>(row.CategoryIdsJson, JsonOptions) ?? [];
        PomodoroPlan? pomodoro = row.PomodoroTotalMilliseconds is long total &&
                                  row.PomodoroWorkMilliseconds is long work &&
                                  row.PomodoroBreakMilliseconds is long pause &&
                                  row.PomodoroBufferMilliseconds is long buffer
            ? new PomodoroPlan(total, work, pause, buffer, row.PomodoroBreaksBillable)
            : null;
        return new(
            row.Id,
            row.SessionId,
            (TimingStatus)row.Status,
            (TimingMode)row.TimingMode,
            SessionAssociations.Create(row.StreamId, categoryIds, row.ProjectId),
            new BillingSnapshot(row.IsBillable, row.HourlyRateMinorUnits, row.CurrencyCode, null),
            row.RoundingIncrementMinutes,
            (RoundingRule)row.RoundingRule,
            row.StartedUtcMilliseconds,
            row.CurrentSegmentStartedUtcMilliseconds,
            row.RequestedDurationMilliseconds,
            row.ScheduledEndUtcMilliseconds,
            row.AccumulatedWorkMilliseconds,
            row.AccumulatedBreakMilliseconds,
            pomodoro,
            row.PausedFromStatus is int paused ? (TimingStatus)paused : null,
            row.Revision);
    }

    private static CompletedSession ToDomain(SessionRow row) => new(
        row.Id,
        (SessionOrigin)row.Origin,
        (TimingMode)row.TimingMode,
        SessionAssociations.Create(row.StreamId, row.Categories.Select(item => item.CategoryId), row.ProjectId),
        row.StartUtcMilliseconds,
        row.EndUtcMilliseconds,
        row.TimeZoneId,
        row.StartUtcOffsetMinutes,
        row.EndUtcOffsetMinutes,
        new RoundingSnapshot(
            row.RawDurationMilliseconds,
            row.EffectiveDurationMilliseconds,
            row.RoundingIncrementMinutes,
            (RoundingRule)row.RoundingRule),
        row.RequestedDurationMilliseconds,
        new BillingSnapshot(
            row.IsBillable,
            row.HourlyRateMinorUnits,
            row.CurrencyCode,
            row.EstimatedEarningMinorUnits),
        row.Description,
        row.CompletionPending,
        row.CreatedUtcMilliseconds,
        row.UpdatedUtcMilliseconds);
}
