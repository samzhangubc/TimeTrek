using Microsoft.EntityFrameworkCore;
using TimeTrek.Application.Reporting;

namespace TimeTrek.Infrastructure.Persistence;

public sealed class SqliteReportingStore(IDbContextFactory<TimeTrekDbContext> contextFactory) : IReportingStore
{
    public async ValueTask<StatsSnapshot> GetStatsAsync(
        ReportingQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.EndUtcMilliseconds <= query.StartUtcMilliseconds)
        {
            throw new ArgumentOutOfRangeException(nameof(query));
        }

        await using TimeTrekDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        IQueryable<SessionRow> all = context.Sessions.AsNoTracking().Where(row => !row.IsDeleted && !row.IsDraft);
        long lifetime = await all.SumAsync(
            row => query.Basis == ReportingBasis.Raw ? row.RawDurationMilliseconds : row.EffectiveDurationMilliseconds,
            cancellationToken).ConfigureAwait(false);
        lifetime = checked(lifetime + await context.Adjustments.AsNoTracking().Where(row => !row.IsDeleted).SumAsync(
            row => query.Basis == ReportingBasis.Raw ? row.RawDurationMilliseconds : row.EffectiveDurationMilliseconds,
            cancellationToken).ConfigureAwait(false));

        IQueryable<SessionRow> source = all.Where(row =>
            row.EndUtcMilliseconds > query.StartUtcMilliseconds && row.StartUtcMilliseconds < query.EndUtcMilliseconds);
        if (query.StreamIds is { Count: > 0 })
        {
            Guid[] ids = query.StreamIds.ToArray();
            source = source.Where(row => row.StreamId != null && ids.Contains(row.StreamId.Value));
        }

        if (query.ProjectIds is { Count: > 0 })
        {
            Guid[] ids = query.ProjectIds.ToArray();
            source = source.Where(row => row.ProjectId != null && ids.Contains(row.ProjectId.Value));
        }

        if (query.CategoryIds is { Count: > 0 })
        {
            Guid[] ids = query.CategoryIds.ToArray();
            source = source.Where(row => row.Categories.Any(join => ids.Contains(join.CategoryId)));
        }

        if (query.IsBillable is bool billable)
        {
            source = source.Where(row => row.IsBillable == billable);
        }

        List<SessionRow> rows = await source.ToListAsync(cancellationToken).ConfigureAwait(false);
        long firstDay = FloorDays(query.StartUtcMilliseconds);
        long lastDay = FloorDays(query.EndUtcMilliseconds - 1);
        IQueryable<AdjustmentRow> adjustmentSource = context.Adjustments.AsNoTracking().Where(row =>
            !row.IsDeleted && row.LocalDateUnixDays >= firstDay && row.LocalDateUnixDays <= lastDay);
        if (query.StreamIds is { Count: > 0 })
        {
            Guid[] ids = query.StreamIds.ToArray();
            adjustmentSource = adjustmentSource.Where(row => row.StreamId != null && ids.Contains(row.StreamId.Value));
        }

        if (query.ProjectIds is { Count: > 0 })
        {
            Guid[] ids = query.ProjectIds.ToArray();
            adjustmentSource = adjustmentSource.Where(row => row.ProjectId != null && ids.Contains(row.ProjectId.Value));
        }

        if (query.CategoryIds is { Count: > 0 })
        {
            Guid[] ids = query.CategoryIds.ToArray();
            adjustmentSource = adjustmentSource.Where(row => row.Categories.Any(join => ids.Contains(join.CategoryId)));
        }

        if (query.IsBillable is bool adjustmentBillable)
        {
            adjustmentSource = adjustmentSource.Where(row => row.IsBillable == adjustmentBillable);
        }

        List<AdjustmentRow> adjustments = await adjustmentSource.ToListAsync(cancellationToken).ConfigureAwait(false);
        long selected = checked(rows.Sum(row => Duration(row, query.Basis)) + adjustments.Sum(row => Duration(row, query.Basis)));
        long longest = rows.Count == 0 ? 0 : rows.Max(row => Duration(row, query.Basis));
        long rangeDays = Math.Max(1, (long)Math.Ceiling((query.EndUtcMilliseconds - query.StartUtcMilliseconds) / 86_400_000d));

        List<DailyTotal> daily = rows.Select(row => new DailyTotal(
                FloorDays(row.StartUtcMilliseconds + row.StartUtcOffsetMinutes * 60_000L),
                row.RawDurationMilliseconds,
                row.EffectiveDurationMilliseconds,
                1))
            .Concat(adjustments.Select(row => new DailyTotal(
                row.LocalDateUnixDays,
                row.RawDurationMilliseconds,
                row.EffectiveDurationMilliseconds,
                0)))
            .GroupBy(item => item.LocalDateUnixDays)
            .Select(group => new DailyTotal(group.Key, group.Sum(item => item.RawMilliseconds), group.Sum(item => item.EffectiveMilliseconds), group.Sum(item => item.SessionCount)))
            .OrderBy(item => item.LocalDateUnixDays)
            .ToList();

        Dictionary<Guid, string> streamNames = await context.Streams.AsNoTracking()
            .ToDictionaryAsync(row => row.Id, row => row.Name, cancellationToken).ConfigureAwait(false);
        Dictionary<Guid, string> projectNames = await context.Projects.AsNoTracking()
            .ToDictionaryAsync(row => row.Id, row => row.Name, cancellationToken).ConfigureAwait(false);
        List<RankedTotal> streams = Rank(rows, adjustments, row => row.StreamId, row => row.StreamId, streamNames, selected, query.Basis, "Uncategorized");
        List<RankedTotal> projects = Rank(rows, adjustments, row => row.ProjectId, row => row.ProjectId, projectNames, selected, query.Basis, "No Project");
        Dictionary<string, long> earnings = rows.Cast<object>().Concat(adjustments)
            .Select(item => item is SessionRow session
                ? new { session.CurrencyCode, session.EstimatedEarningMinorUnits }
                : new { ((AdjustmentRow)item).CurrencyCode, ((AdjustmentRow)item).EstimatedEarningMinorUnits })
            .Where(row => row.EstimatedEarningMinorUnits.HasValue && row.CurrencyCode != null)
            .GroupBy(row => row.CurrencyCode!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Sum(row => row.EstimatedEarningMinorUnits!.Value), StringComparer.Ordinal);

        return new(
            lifetime,
            selected,
            selected / rangeDays,
            longest,
            rows.Count,
            daily,
            streams,
            projects,
            earnings);
    }

    private static long Duration(SessionRow row, ReportingBasis basis) =>
        basis == ReportingBasis.Raw ? row.RawDurationMilliseconds : row.EffectiveDurationMilliseconds;

    private static long Duration(AdjustmentRow row, ReportingBasis basis) =>
        basis == ReportingBasis.Raw ? row.RawDurationMilliseconds : row.EffectiveDurationMilliseconds;

    private static long FloorDays(long milliseconds)
    {
        long quotient = Math.DivRem(milliseconds, 86_400_000L, out long remainder);
        return remainder < 0 ? quotient - 1 : quotient;
    }

    private static List<RankedTotal> Rank(
        IEnumerable<SessionRow> rows,
        IEnumerable<AdjustmentRow> adjustments,
        Func<SessionRow, Guid?> selector,
        Func<AdjustmentRow, Guid?> adjustmentSelector,
        IReadOnlyDictionary<Guid, string> names,
        long total,
        ReportingBasis basis,
        string missingName) => rows.Select(row => (Id: selector(row), Duration: Duration(row, basis)))
        .Concat(adjustments.Select(row => (Id: adjustmentSelector(row), Duration: Duration(row, basis))))
        .GroupBy(item => item.Id)
        .Select(group => new RankedTotal(
            group.Key,
            group.Key is Guid id ? names.GetValueOrDefault(id, "Unknown") : missingName,
            group.Sum(row => row.Duration),
            total == 0 ? 0 : group.Sum(row => row.Duration) * 100d / total))
        .OrderByDescending(item => item.DurationMilliseconds)
        .Take(50)
        .ToList();
}
