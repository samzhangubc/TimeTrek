namespace TimeTrek.Application.Reporting;

public enum ReportingBasis
{
    Raw,
    Effective,
}

public sealed record ReportingQuery(
    long StartUtcMilliseconds,
    long EndUtcMilliseconds,
    ReportingBasis Basis,
    IReadOnlySet<Guid>? StreamIds = null,
    IReadOnlySet<Guid>? CategoryIds = null,
    IReadOnlySet<Guid>? ProjectIds = null,
    bool? IsBillable = null);

public sealed record DailyTotal(long LocalDateUnixDays, long RawMilliseconds, long EffectiveMilliseconds, int SessionCount);

public sealed record RankedTotal(Guid? Id, string Name, long DurationMilliseconds, double Percentage);

public sealed record StatsSnapshot(
    long LifetimeMilliseconds,
    long SelectedRangeMilliseconds,
    long DailyAverageMilliseconds,
    long LongestSessionMilliseconds,
    int SessionCount,
    IReadOnlyList<DailyTotal> Daily,
    IReadOnlyList<RankedTotal> Streams,
    IReadOnlyList<RankedTotal> Projects,
    IReadOnlyDictionary<string, long> EarningsByCurrency);

public interface IReportingStore
{
    ValueTask<StatsSnapshot> GetStatsAsync(ReportingQuery query, CancellationToken cancellationToken = default);
}
