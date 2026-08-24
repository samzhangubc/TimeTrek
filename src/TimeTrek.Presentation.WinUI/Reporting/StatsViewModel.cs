using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TimeTrek.Application.Reporting;

namespace TimeTrek.Presentation.WinUI.Reporting;

public sealed record RankingRow(string Name, string Duration, string Percentage);
public sealed record DailyActivityRow(string Date, string Duration, double BarHeight);

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

    public async ValueTask LoadAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset end = timeProvider.GetUtcNow();
        DateTimeOffset start = end.AddMonths(-4);
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
        DailyActivity.Clear();
        foreach (DailyTotal day in stats.Daily.TakeLast(120))
        {
            long value = Basis == ReportingBasis.Raw ? day.RawMilliseconds : day.EffectiveMilliseconds;
            DateTime date = DateTime.UnixEpoch.AddDays(day.LocalDateUnixDays);
            DailyActivity.Add(new DailyActivityRow(
                date.ToString("MMM d", System.Globalization.CultureInfo.CurrentCulture),
                Format(value),
                Math.Max(2, value * 190d / Math.Max(1, maximumDay))));
        }

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

    private static string Format(long milliseconds)
    {
        TimeSpan duration = TimeSpan.FromMilliseconds(milliseconds);
        return $"{(long)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
    }
}
