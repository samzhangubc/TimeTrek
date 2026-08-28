using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ThymeMe.Application.Reporting;

namespace ThymeMe.Presentation.WinUI.Reporting;

public sealed record RankingRow(string Name, string Duration, string Percentage);
public sealed record DailyActivityRow(string Date, string Duration, double BarHeight);

public enum StatsRangePreset
{
    Week,
    Month,
    FourMonths,
    Custom,
}

public enum StatsViewMode
{
    Daily,
    Weekly,
    Cumulative,
    Timeline,
}

public sealed partial class StatsViewModel(IReportingStore reportingStore, TimeProvider timeProvider) : ObservableObject
{
    public ObservableCollection<RankingRow> Streams { get; } = [];

    public ObservableCollection<RankingRow> Projects { get; } = [];

    public ObservableCollection<DailyActivityRow> DailyActivity { get; } = [];

    [ObservableProperty]
    public partial string LifetimeTotal { get; set; } = "00:00:00";

    [ObservableProperty]
    public partial string RangeTotal { get; set; } = "00:00:00";

    [ObservableProperty]
    public partial string DailyAverage { get; set; } = "00:00:00";

    [ObservableProperty]
    public partial string LongestSession { get; set; } = "00:00:00";

    [ObservableProperty]
    public partial string SessionCount { get; set; } = "0";

    [ObservableProperty]
    public partial ReportingBasis Basis { get; set; } = ReportingBasis.Raw;

    [ObservableProperty]
    public partial string Earnings { get; set; } = "No billable earnings";

    [ObservableProperty]
    public partial StatsRangePreset RangePreset { get; set; } = StatsRangePreset.FourMonths;

    [ObservableProperty]
    public partial StatsViewMode ViewMode { get; set; } = StatsViewMode.Daily;

    [ObservableProperty]
    public partial string RangeLabel { get; set; } = string.Empty;

    public DateTimeOffset RangeStart { get; private set; }

    public DateTimeOffset RangeEnd { get; private set; }

    public async ValueTask LoadAsync(CancellationToken cancellationToken = default)
    {
        EnsureRange();
        DateTimeOffset start = RangeStart;
        DateTimeOffset end = RangeEnd;
        StatsSnapshot stats = await reportingStore.GetStatsAsync(new ReportingQuery(
            start.ToUnixTimeMilliseconds(),
            end.ToUnixTimeMilliseconds(),
            Basis), cancellationToken);
        LifetimeTotal = Format(stats.LifetimeMilliseconds);
        RangeTotal = Format(stats.SelectedRangeMilliseconds);
        DailyAverage = Format(stats.DailyAverageMilliseconds);
        LongestSession = Format(stats.LongestSessionMilliseconds);
        SessionCount = stats.SessionCount.ToString(System.Globalization.CultureInfo.CurrentCulture);
        long maximumDay = stats.Daily.Count == 0 ? 1 : stats.Daily.Max(item =>
            Basis == ReportingBasis.Raw ? item.RawMilliseconds : item.EffectiveMilliseconds);
        BuildActivity(stats, maximumDay);

        Earnings = stats.EarningsByCurrency.Count == 0
            ? "No billable earnings"
            : string.Join(" · ", stats.EarningsByCurrency.OrderBy(item => item.Key).Select(item => $"{item.Key} {item.Value / 100d:N2}"));
        Streams.Clear();
        Projects.Clear();
        foreach (RankedTotal row in stats.Streams.Take(5))
        {
            Streams.Add(new RankingRow(row.Name, Format(row.DurationMilliseconds), $"{row.Percentage:0.#}%"));
        }

        foreach (RankedTotal row in stats.Projects.Take(5))
        {
            Projects.Add(new RankingRow(row.Name, Format(row.DurationMilliseconds), $"{row.Percentage:0.#}%"));
        }
    }

    public void SetPreset(StatsRangePreset preset)
    {
        RangePreset = preset;
        DateTimeOffset now = timeProvider.GetLocalNow();
        RangeEnd = now;
        RangeStart = preset switch
        {
            StatsRangePreset.Week => now.AddDays(-7),
            StatsRangePreset.Month => now.AddMonths(-1),
            _ => now.AddMonths(-4),
        };
        UpdateRangeLabel();
    }

    public void SetCustomRange(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
        {
            throw new ArgumentOutOfRangeException(nameof(end), "The end of the range must be after its start.");
        }

        RangePreset = StatsRangePreset.Custom;
        RangeStart = start;
        RangeEnd = end;
        UpdateRangeLabel();
    }

    public void MoveRange(int direction)
    {
        if (direction is not (-1 or 1))
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }

        TimeSpan customSpan = RangeEnd - RangeStart;
        (RangeStart, RangeEnd) = RangePreset switch
        {
            StatsRangePreset.Week => (RangeStart.AddDays(7 * direction), RangeEnd.AddDays(7 * direction)),
            StatsRangePreset.Month => (RangeStart.AddMonths(direction), RangeEnd.AddMonths(direction)),
            StatsRangePreset.FourMonths => (RangeStart.AddMonths(4 * direction), RangeEnd.AddMonths(4 * direction)),
            _ => (RangeStart.AddTicks(customSpan.Ticks * direction), RangeEnd.AddTicks(customSpan.Ticks * direction)),
        };
        UpdateRangeLabel();
    }

    private void EnsureRange()
    {
        if (RangeEnd == default)
        {
            SetPreset(RangePreset);
        }
    }

    private void BuildActivity(StatsSnapshot stats, long maximumDay)
    {
        IEnumerable<(long Day, long Value)> values = stats.Daily.Select(day =>
            (day.LocalDateUnixDays, Basis == ReportingBasis.Raw ? day.RawMilliseconds : day.EffectiveMilliseconds));
        if (ViewMode == StatsViewMode.Weekly)
        {
            values = values.GroupBy(item => item.Day / 7)
                .Select(group => (group.Min(item => item.Day), group.Sum(item => item.Value)));
        }
        else if (ViewMode == StatsViewMode.Cumulative)
        {
            long running = 0;
            values = values.OrderBy(item => item.Day).Select(item => (item.Day, running = checked(running + item.Value))).ToList();
        }

        List<(long Day, long Value)> rows = values.OrderBy(item => item.Day).TakeLast(366).ToList();
        long maximum = rows.Count == 0 ? maximumDay : Math.Max(1, rows.Max(item => item.Value));
        DailyActivity.Clear();
        foreach ((long day, long value) in rows)
        {
            DateTime date = DateTime.UnixEpoch.AddDays(day);
            DailyActivity.Add(new DailyActivityRow(
                date.ToString(ViewMode == StatsViewMode.Weekly ? "MMM d" : "MMM d", System.Globalization.CultureInfo.CurrentCulture),
                Format(value),
                Math.Max(2, value * 190d / maximum)));
        }
    }

    private void UpdateRangeLabel() => RangeLabel =
        $"{RangeStart.ToString("d", System.Globalization.CultureInfo.CurrentCulture)} – {RangeEnd.ToString("d", System.Globalization.CultureInfo.CurrentCulture)}";

    private static string Format(long milliseconds)
    {
        TimeSpan duration = TimeSpan.FromMilliseconds(milliseconds);
        return $"{(long)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
    }
}
