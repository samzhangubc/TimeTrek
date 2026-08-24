using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeTrek.Application.History;
using TimeTrek.Application.Settings;
using TimeTrek.Domain.Organization;
using TimeTrek.Domain.Reporting;
using TimeTrek.Domain.Timing;

namespace TimeTrek.Presentation.WinUI.History;

public sealed record HistoryRow(
    Guid Id,
    string Date,
    string Time,
    string Duration,
    string Stream,
    string Categories,
    string Project,
    string Description,
    string Origin,
    bool IsDeleted,
    string DeletionDeadline);

public sealed partial class HistoryViewModel(
    IHistoryStore historyStore,
    HistoryService historyService,
    IAppSettingsStore settingsStore,
    TimeProvider timeProvider) : ObservableObject
{
    public ObservableCollection<HistoryRow> Items { get; } = [];

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Summary { get; set; } = "0 Sessions · 00:00:00";

    [ObservableProperty]
    public partial bool ShowRecentlyDeleted { get; set; }

    public async ValueTask LoadAsync(CancellationToken cancellationToken = default)
    {
        Application.History.HistoryPage page = await historyStore.QueryAsync(new HistoryQuery(
            Search: Search,
            IncludeDeleted: ShowRecentlyDeleted,
            Limit: 500), cancellationToken);
        Items.Clear();
        foreach (HistoryItem item in page.Items)
        {
            DateTimeOffset start = DateTimeOffset.FromUnixTimeMilliseconds(item.StartUtcMilliseconds).ToLocalTime();
            Items.Add(new HistoryRow(
                item.Id,
                start.ToString("d", System.Globalization.CultureInfo.CurrentCulture),
                start.ToString("t", System.Globalization.CultureInfo.CurrentCulture),
                FormatDuration(item.EffectiveDurationMilliseconds),
                item.StreamName ?? "Unassigned",
                string.Join(", ", item.CategoryNames),
                item.ProjectName ?? string.Empty,
                item.Description ?? string.Empty,
                item.Origin.ToString(),
                item.IsDeleted,
                item.PurgeAfterUtcMilliseconds is long purge
                    ? DateTimeOffset.FromUnixTimeMilliseconds(purge).ToLocalTime().ToString("g", System.Globalization.CultureInfo.CurrentCulture)
                    : string.Empty));
        }

        Summary = $"{page.TotalCount} Sessions · {FormatDuration(page.SignedTotalDurationMilliseconds)}";
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    public ValueTask<CompletedSession?> GetSessionAsync(Guid id, CancellationToken cancellationToken = default) =>
        historyStore.GetSessionAsync(id, cancellationToken);

    public async ValueTask<string?> AddManualAsync(
        string durationText,
        string? description,
        CancellationToken cancellationToken = default)
    {
        DurationParseResult duration = DurationParser.Parse(durationText);
        if (!duration.IsValid)
        {
            return duration.Error;
        }

        AppSettings settings = await settingsStore.LoadAsync(cancellationToken);
        long end = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        Application.Common.OperationResult<CompletedSession> result = await historyService.AddManualAsync(
            new AddManualSessionRequest(
                checked(end - duration.Milliseconds), end, SessionAssociations.Empty, false, null, null,
                settings.RoundingEnabled ? settings.RoundingIncrementMinutes : null,
                settings.RoundingEnabled ? settings.RoundingRule : RoundingRule.None,
                description, false), cancellationToken);
        await LoadAsync(cancellationToken);
        return result.Error?.Message;
    }

    public async ValueTask<string?> AddAdjustmentAsync(
        string durationText,
        string? description,
        CancellationToken cancellationToken = default)
    {
        DurationParseResult duration = DurationParser.Parse(durationText);
        if (!duration.IsValid)
        {
            return duration.Error;
        }

        AppSettings settings = await settingsStore.LoadAsync(cancellationToken);
        DateOnly date = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        long unixDays = date.DayNumber - new DateOnly(1970, 1, 1).DayNumber;
        Application.Common.OperationResult<NegativeAdjustment> result = await historyService.AddAdjustmentAsync(
            new AddAdjustmentRequest(
                unixDays, checked(-duration.Milliseconds), SessionAssociations.Empty, false, null, null,
                settings.RoundingEnabled ? settings.RoundingIncrementMinutes : null,
                settings.RoundingEnabled ? settings.RoundingRule : RoundingRule.None,
                description), cancellationToken);
        await LoadAsync(cancellationToken);
        return result.Error?.Message;
    }

    public async ValueTask DeleteOrRestoreAsync(HistoryRow row, CancellationToken cancellationToken = default)
    {
        if (row.IsDeleted)
        {
            await historyService.RestoreAsync(row.Id, cancellationToken);
        }
        else
        {
            AppSettings settings = await settingsStore.LoadAsync(cancellationToken);
            await historyService.DeleteAsync(row.Id, settings.RecentlyDeletedRetentionDays, cancellationToken);
        }

        await LoadAsync(cancellationToken);
    }

    public async ValueTask<string?> UpdateDescriptionAsync(
        Guid id,
        string? description,
        CancellationToken cancellationToken = default)
    {
        Application.Common.OperationResult<bool> result = await historyService.UpdateDescriptionAsync(id, description, cancellationToken);
        await LoadAsync(cancellationToken);
        return result.Error?.Message;
    }

    private static string FormatDuration(long milliseconds)
    {
        string sign = milliseconds < 0 ? "−" : string.Empty;
        TimeSpan duration = TimeSpan.FromMilliseconds(Math.Abs(milliseconds));
        return $"{sign}{(long)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
    }
}
