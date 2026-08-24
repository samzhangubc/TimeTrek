using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeTrek.Application.History;
using TimeTrek.Application.Organization;
using TimeTrek.Application.Reporting;
using TimeTrek.Application.Settings;
using TimeTrek.Domain.Organization;
using TimeTrek.Domain.Reporting;
using TimeTrek.Domain.Timing;

namespace TimeTrek.Presentation.WinUI.Home;

public sealed record StreamCard(
    Guid Id,
    string Name,
    string Color,
    string Total,
    string Today,
    string ThisWeek,
    string Budget,
    double ChannelWidth);

public sealed partial class HomeViewModel(
    OrganizationService organizationService,
    IReportingStore reportingStore,
    HistoryService historyService,
    IAppSettingsStore settingsStore,
    TimeProvider timeProvider) : ObservableObject
{
    public ObservableCollection<StreamCard> Streams { get; } = [];

    [ObservableProperty]
    public partial string NewStreamName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public async ValueTask LoadAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StreamDefinition> streams = await organizationService.ListStreamsAsync(false, cancellationToken);
        AppSettings appSettings = await settingsStore.LoadAsync(cancellationToken);
        DateTimeOffset now = timeProvider.GetLocalNow();
        DateTimeOffset todayStart = new(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);
        int dayOffset = (7 + (int)todayStart.DayOfWeek - (int)System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek) % 7;
        DateTimeOffset weekStart = todayStart.AddDays(-dayOffset);
        StatsSnapshot lifetime = await reportingStore.GetStatsAsync(new ReportingQuery(
            DateTimeOffset.MinValue.ToUnixTimeMilliseconds(),
            now.ToUnixTimeMilliseconds() + 1,
            ReportingBasis.Raw), cancellationToken);
        StatsSnapshot today = await reportingStore.GetStatsAsync(new ReportingQuery(
            todayStart.ToUnixTimeMilliseconds(),
            now.ToUnixTimeMilliseconds() + 1,
            ReportingBasis.Raw), cancellationToken);
        StatsSnapshot week = await reportingStore.GetStatsAsync(new ReportingQuery(
            weekStart.ToUnixTimeMilliseconds(),
            now.ToUnixTimeMilliseconds() + 1,
            ReportingBasis.Raw), cancellationToken);
        Dictionary<Guid, long> lifetimeByStream = lifetime.Streams.Where(item => item.Id.HasValue).ToDictionary(item => item.Id!.Value, item => item.DurationMilliseconds);
        Dictionary<Guid, long> todayByStream = today.Streams.Where(item => item.Id.HasValue).ToDictionary(item => item.Id!.Value, item => item.DurationMilliseconds);
        Dictionary<Guid, long> weekByStream = week.Streams.Where(item => item.Id.HasValue).ToDictionary(item => item.Id!.Value, item => item.DurationMilliseconds);
        Streams.Clear();
        foreach (StreamDefinition stream in streams)
        {
            Streams.Add(new StreamCard(
                stream.Id,
                stream.Name,
                stream.Color ?? "#2F81F7",
                Format(lifetimeByStream.GetValueOrDefault(stream.Id)),
                Format(todayByStream.GetValueOrDefault(stream.Id)),
                Format(weekByStream.GetValueOrDefault(stream.Id)),
                stream.Budget is null ? string.Empty : "0%",
                appSettings.SharedChannelWidth));
        }
    }

    [RelayCommand]
    private async Task CreateStreamAsync()
    {
        Application.Common.OperationResult<StreamDefinition> result =
            await organizationService.CreateStreamAsync(NewStreamName);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error!.Message;
            return;
        }

        NewStreamName = string.Empty;
        ErrorMessage = null;
        await LoadAsync();
    }

    private static string Format(long milliseconds)
    {
        TimeSpan duration = TimeSpan.FromMilliseconds(milliseconds);
        return $"{(long)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
    }

    public async ValueTask<string?> CreateCategoryAsync(
        string name,
        Guid? streamId,
        bool billable,
        long? hourlyRateMinorUnits,
        string? currencyCode,
        CancellationToken cancellationToken = default)
    {
        Application.Common.OperationResult<CategoryDefinition> result = await organizationService.CreateCategoryAsync(
            name, streamId, billable, hourlyRateMinorUnits, currencyCode, cancellationToken);
        return result.Error?.Message;
    }

    public async ValueTask<string?> CreateProjectAsync(
        string name,
        Guid? streamId,
        CancellationToken cancellationToken = default)
    {
        Application.Common.OperationResult<ProjectDefinition> result = await organizationService.CreateProjectAsync(
            name, streamId, cancellationToken);
        return result.Error?.Message;
    }

    public async ValueTask ArchiveStreamAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await organizationService.ArchiveAsync(OrganizationKind.Stream, id, cancellationToken);
        await LoadAsync(cancellationToken);
    }

    public async ValueTask<string?> AddManualAsync(
        Guid streamId,
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
                checked(end - duration.Milliseconds),
                end,
                SessionAssociations.Create(streamId, null, null),
                false,
                null,
                null,
                settings.RoundingEnabled ? settings.RoundingIncrementMinutes : null,
                settings.RoundingEnabled ? settings.RoundingRule : RoundingRule.None,
                description,
                false),
            cancellationToken);
        if (result.IsSuccess)
        {
            await LoadAsync(cancellationToken);
        }

        return result.Error?.Message;
    }

    public async ValueTask<string?> AddAdjustmentAsync(
        Guid streamId,
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
                unixDays,
                checked(-duration.Milliseconds),
                SessionAssociations.Create(streamId, null, null),
                false,
                null,
                null,
                settings.RoundingEnabled ? settings.RoundingIncrementMinutes : null,
                settings.RoundingEnabled ? settings.RoundingRule : RoundingRule.None,
                description),
            cancellationToken);
        if (result.IsSuccess)
        {
            await LoadAsync(cancellationToken);
        }

        return result.Error?.Message;
    }
}
