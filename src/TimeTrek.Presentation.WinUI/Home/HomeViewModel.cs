using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeTrek.Application.History;
using TimeTrek.Application.Organization;
using TimeTrek.Application.Reporting;
using TimeTrek.Application.Settings;
using TimeTrek.Application.Timing;
using TimeTrek.Domain.Organization;
using TimeTrek.Domain.Reporting;
using TimeTrek.Domain.Timing;

namespace TimeTrek.Presentation.WinUI.Home;

public sealed record CategoryOption(Guid Id, string Name);
public sealed record ProjectOption(Guid Id, string Name);

public sealed partial class StreamCard : ObservableObject
{
    public required Guid Id { get; init; }
    public required string Name { get; set; }
    public required string Color { get; set; }
    public required string Total { get; init; }
    public required string Today { get; init; }
    public required string ThisWeek { get; init; }
    public required double ChannelWidth { get; init; }
    public required IReadOnlyList<CategoryOption> CategoryOptions { get; init; }
    public required IReadOnlyList<ProjectOption> ProjectOptions { get; init; }
    public required HashSet<Guid> SelectedCategoryIds { get; init; }
    public TimeBudget? Budget { get; set; }

    [ObservableProperty] public partial Guid? SelectedProjectId { get; set; }
    [ObservableProperty] public partial string CategorySummary { get; set; } = "Choose categories";
    [ObservableProperty] public partial string TimerText { get; set; } = "00:00:00";
    [ObservableProperty] public partial double SessionFillHeight { get; set; }
    [ObservableProperty] public partial string SessionProgressText { get; set; } = string.Empty;
    [ObservableProperty] public partial double BudgetFillHeight { get; set; }
    [ObservableProperty] public partial string BudgetProgressText { get; set; } = string.Empty;
    [ObservableProperty] public partial bool HasBudget { get; set; }
}

public sealed partial class HomeViewModel(
    OrganizationService organizationService,
    IReportingStore reportingStore,
    HistoryService historyService,
    IAppSettingsStore settingsStore,
    TimingCoordinator timingCoordinator,
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
        IReadOnlyList<CategoryDefinition> categories = await organizationService.ListCategoriesAsync(false, cancellationToken);
        IReadOnlyList<ProjectDefinition> projects = await organizationService.ListProjectsAsync(false, cancellationToken);
        AppSettings appSettings = await settingsStore.LoadAsync(cancellationToken);
        DateTimeOffset now = timeProvider.GetLocalNow();
        DateTimeOffset todayStart = new(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);
        int dayOffset = (7 + (int)todayStart.DayOfWeek - (int)System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek) % 7;
        DateTimeOffset weekStart = todayStart.AddDays(-dayOffset);
        DateTimeOffset monthStart = new(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
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
        StatsSnapshot month = await reportingStore.GetStatsAsync(new ReportingQuery(
            monthStart.ToUnixTimeMilliseconds(),
            now.ToUnixTimeMilliseconds() + 1,
            ReportingBasis.Raw), cancellationToken);
        Dictionary<Guid, long> lifetimeByStream = lifetime.Streams.Where(item => item.Id.HasValue).ToDictionary(item => item.Id!.Value, item => item.DurationMilliseconds);
        Dictionary<Guid, long> todayByStream = today.Streams.Where(item => item.Id.HasValue).ToDictionary(item => item.Id!.Value, item => item.DurationMilliseconds);
        Dictionary<Guid, long> weekByStream = week.Streams.Where(item => item.Id.HasValue).ToDictionary(item => item.Id!.Value, item => item.DurationMilliseconds);
        Dictionary<Guid, long> monthByStream = month.Streams.Where(item => item.Id.HasValue).ToDictionary(item => item.Id!.Value, item => item.DurationMilliseconds);
        TimingSnapshot? active = await timingCoordinator.GetActiveAsync(cancellationToken);
        Streams.Clear();
        foreach (StreamDefinition stream in streams)
        {
            long budgetActual = stream.Budget?.ResetPeriod switch
            {
                BudgetResetPeriod.Weekly => weekByStream.GetValueOrDefault(stream.Id),
                BudgetResetPeriod.Monthly => monthByStream.GetValueOrDefault(stream.Id),
                _ => lifetimeByStream.GetValueOrDefault(stream.Id),
            };
            Guid[] selectedCategoryIds = appSettings.StreamCategoryDefaults.GetValueOrDefault(stream.Id) ?? [];
            HashSet<Guid> validCategoryIds = categories
                .Where(item => item.StreamId is null || item.StreamId == stream.Id)
                .Select(item => item.Id)
                .ToHashSet();
            HashSet<Guid> selectedCategories = selectedCategoryIds.Where(validCategoryIds.Contains).ToHashSet();
            IReadOnlyList<CategoryOption> categoryOptions = categories
                .Where(item => item.StreamId is null || item.StreamId == stream.Id)
                .Select(item => new CategoryOption(item.Id, item.Name))
                .ToList();
            IReadOnlyList<ProjectOption> projectOptions = projects
                .Where(item => item.StreamId is null || item.StreamId == stream.Id)
                .Select(item => new ProjectOption(item.Id, item.Name))
                .ToList();
            Guid? selectedProject = appSettings.StreamProjectDefaults.GetValueOrDefault(stream.Id);
            if (selectedProject.HasValue && projectOptions.All(item => item.Id != selectedProject))
            {
                selectedProject = null;
            }

            double budgetRatio = stream.Budget is null
                ? 0
                : Math.Clamp(budgetActual / (double)stream.Budget.DurationMilliseconds, 0, 1);
            StreamCard card = new()
            {
                Id = stream.Id,
                Name = stream.Name,
                Color = stream.Color ?? "#2F81F7",
                Total = Format(lifetimeByStream.GetValueOrDefault(stream.Id)),
                Today = Format(todayByStream.GetValueOrDefault(stream.Id)),
                ThisWeek = Format(weekByStream.GetValueOrDefault(stream.Id)),
                ChannelWidth = Math.Clamp(appSettings.SharedChannelWidth * appSettings.IconScale, 176, 420),
                CategoryOptions = categoryOptions,
                ProjectOptions = projectOptions,
                SelectedCategoryIds = selectedCategories,
                SelectedProjectId = selectedProject,
                CategorySummary = CategorySummary(categoryOptions, selectedCategories),
                TimerText = Format(lifetimeByStream.GetValueOrDefault(stream.Id)),
                Budget = stream.Budget,
                HasBudget = stream.Budget is not null,
                BudgetFillHeight = budgetRatio * 120,
                BudgetProgressText = stream.Budget is null
                    ? string.Empty
                    : $"{Format(budgetActual)} of {Format(stream.Budget.DurationMilliseconds)} · {budgetRatio:P0}",
            };
            ApplyActive(card, active);
            Streams.Add(card);
        }
    }

    public async ValueTask RefreshTimingAsync(CancellationToken cancellationToken = default)
    {
        TimingSnapshot? active = await timingCoordinator.GetActiveAsync(cancellationToken);
        foreach (StreamCard card in Streams)
        {
            ApplyActive(card, active);
        }
    }

    public async ValueTask SaveStreamDefaultsAsync(StreamCard card, CancellationToken cancellationToken = default)
    {
        AppSettings settings = await settingsStore.LoadAsync(cancellationToken);
        Dictionary<Guid, Guid[]> categoryDefaults = new(settings.StreamCategoryDefaults)
        {
            [card.Id] = card.SelectedCategoryIds.Order().ToArray(),
        };
        Dictionary<Guid, Guid?> projectDefaults = new(settings.StreamProjectDefaults)
        {
            [card.Id] = card.SelectedProjectId,
        };
        await settingsStore.SaveAsync(settings with
        {
            StreamCategoryDefaults = categoryDefaults,
            StreamProjectDefaults = projectDefaults,
        }, cancellationToken);
        card.CategorySummary = CategorySummary(card.CategoryOptions, card.SelectedCategoryIds);
    }

    public async ValueTask<string?> UpdateStreamAsync(
        StreamCard card,
        string name,
        string? color,
        TimeBudget? budget,
        CancellationToken cancellationToken = default)
    {
        Application.Common.OperationResult<StreamDefinition> result = await organizationService.UpdateStreamAsync(
            card.Id, name, color, budget, cancellationToken);
        if (result.IsSuccess)
        {
            await LoadAsync(cancellationToken);
        }

        return result.Error?.Message;
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

    private void ApplyActive(StreamCard card, TimingSnapshot? active)
    {
        if (active?.Associations.StreamId != card.Id)
        {
            card.TimerText = card.Total;
            card.SessionFillHeight = 0;
            card.SessionProgressText = string.Empty;
            return;
        }

        long elapsed = active.ElapsedWorkAt(timeProvider.GetUtcNow().ToUnixTimeMilliseconds());
        double ratio = Math.Clamp(elapsed / (double)Math.Max(1, active.RequestedDurationMilliseconds), 0, 1);
        card.TimerText = Format(elapsed);
        card.SessionFillHeight = ratio * 120;
        card.SessionProgressText = $"Session {ratio:P0}";
    }

    private static string CategorySummary(IReadOnlyList<CategoryOption> options, HashSet<Guid> selected)
    {
        string[] names = options.Where(item => selected.Contains(item.Id)).Select(item => item.Name).ToArray();
        return names.Length switch
        {
            0 => "Choose categories",
            1 => names[0],
            _ => $"{names[0]} +{names.Length - 1}",
        };
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
