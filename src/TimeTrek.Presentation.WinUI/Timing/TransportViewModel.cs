using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TimeTrek.Application.Common;
using TimeTrek.Application.Organization;
using TimeTrek.Application.Settings;
using TimeTrek.Application.Timing;
using TimeTrek.Domain.Organization;
using TimeTrek.Domain.Reporting;
using TimeTrek.Domain.Timing;

namespace TimeTrek.Presentation.WinUI.Timing;

public sealed record StreamOption(Guid Id, string Name);
public sealed record AssociationOption(Guid Id, string Name);

public sealed partial class TransportViewModel(
    TimingCoordinator timingCoordinator,
    IOrganizationStore organizationStore,
    IAppSettingsStore settingsStore,
    TimeProvider timeProvider) : ObservableObject
{
    public ObservableCollection<StreamOption> Streams { get; } = [];
    public ObservableCollection<AssociationOption> Categories { get; } = [];
    public ObservableCollection<AssociationOption> Projects { get; } = [];
    private IReadOnlyList<CategoryDefinition> allCategories = [];
    private IReadOnlyList<ProjectDefinition> allProjects = [];

    [ObservableProperty]
    public partial Guid? SelectedStreamId { get; set; }

    public HashSet<Guid> SelectedCategoryIds { get; } = [];

    [ObservableProperty]
    public partial Guid? SelectedProjectId { get; set; }

    [ObservableProperty]
    public partial string CategorySummary { get; set; } = "Choose categories";

    [ObservableProperty]
    public partial string DurationText { get; set; } = "25";

    [ObservableProperty]
    public partial bool IsPomodoro { get; set; }

    [ObservableProperty]
    public partial int PomodoroWorkMinutes { get; set; } = 25;

    [ObservableProperty]
    public partial int PomodoroBreakMinutes { get; set; } = 5;

    [ObservableProperty]
    public partial string ElapsedText { get; set; } = "00:00:00";

    [ObservableProperty]
    public partial string StateText { get; set; } = "Ready";

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    public partial bool IsPaused { get; set; }

    [ObservableProperty]
    public partial bool IsCompletionPending { get; set; }

    [ObservableProperty]
    public partial bool IsTransitionPending { get; set; }

    [ObservableProperty]
    public partial bool CanSkipInterval { get; set; }

    [ObservableProperty]
    public partial bool CanStart { get; set; } = true;

    [ObservableProperty]
    public partial bool CanPause { get; set; }

    [ObservableProperty]
    public partial bool CanResume { get; set; }

    [ObservableProperty]
    public partial bool CanStop { get; set; }

    [ObservableProperty]
    public partial bool CanApplyDuration { get; set; }

    [ObservableProperty]
    public partial string CompletionDescription { get; set; } = string.Empty;

    private TimingSnapshot? snapshot;
    private Guid? pendingSessionId;

    public async ValueTask LoadAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StreamDefinition> streams = await organizationStore.ListStreamsAsync(false, cancellationToken);
        Streams.Clear();
        foreach (StreamDefinition stream in streams)
        {
            Streams.Add(new StreamOption(stream.Id, stream.Name));
        }

        snapshot = await timingCoordinator.GetActiveAsync(cancellationToken);
        if (snapshot is not null)
        {
            _ = await timingCoordinator.RecoverAsync(cancellationToken);
            snapshot = await timingCoordinator.GetActiveAsync(cancellationToken);
        }
        AppSettings settings = await settingsStore.LoadAsync(cancellationToken);
        PomodoroWorkMinutes = settings.PomodoroWorkMinutes;
        PomodoroBreakMinutes = settings.PomodoroBreakMinutes;
        IsPomodoro = settings.LastTimingMode == TimingMode.Pomodoro;
        ApplySnapshot(snapshot);
        await LoadOldestPendingAsync(cancellationToken);
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        AppSettings settings = await settingsStore.LoadAsync(cancellationToken);
        DurationParseResult parsed = DurationParser.Parse(DurationText);
        if (!parsed.IsValid)
        {
            ErrorMessage = parsed.Error;
            return;
        }

        PomodoroPlan? pomodoro = IsPomodoro
            ? PomodoroPlan.Create(
                parsed.Milliseconds,
                checked(PomodoroWorkMinutes * 60_000L),
                checked(PomodoroBreakMinutes * 60_000L),
                settings.PomodoroBreaksBillable)
            : null;
        StartTimingRequest request = new(
            DurationText,
            IsPomodoro ? TimingMode.Pomodoro : TimingMode.Normal,
            SessionAssociations.Create(SelectedStreamId, SelectedCategoryIds, SelectedProjectId),
            false,
            null,
            null,
            settings.RoundingEnabled ? settings.RoundingIncrementMinutes : null,
            settings.RoundingEnabled ? settings.RoundingRule : RoundingRule.None,
            pomodoro);
        OperationResult<TimingCommandState> result = await timingCoordinator.StartAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error!.Message;
            return;
        }

        ErrorMessage = null;
        snapshot = result.Value!.Snapshot;
        ApplySnapshot(snapshot);
    }

    public async ValueTask StartFromSessionAsync(CompletedSession source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        string duration = Math.Max(1, source.RequestedDurationMilliseconds / 60_000L)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        StartTimingRequest request = new(
            duration,
            TimingMode.Normal,
            source.Associations,
            source.Billing.IsBillable,
            source.Billing.HourlyRateMinorUnits,
            source.Billing.CurrencyCode,
            source.Duration.IncrementMinutes,
            source.Duration.Rule,
            null);
        OperationResult<TimingCommandState> result = await timingCoordinator.StartAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error!.Message;
            return;
        }

        SelectedStreamId = source.Associations.StreamId;
        SelectedCategoryIds.Clear();
        SelectedCategoryIds.UnionWith(source.Associations.CategoryIds);
        SelectedProjectId = source.Associations.ProjectId;
        DurationText = duration;
        ErrorMessage = null;
        snapshot = result.Value!.Snapshot;
        ApplySnapshot(snapshot);
    }

    public async ValueTask PauseAsync(CancellationToken cancellationToken = default)
    {
        OperationResult<TimingSnapshot> result = await timingCoordinator.PauseAsync(cancellationToken);
        ApplyResult(result);
    }

    public async ValueTask ResumeAsync(CancellationToken cancellationToken = default)
    {
        OperationResult<TimingSnapshot> result = await timingCoordinator.ResumeAsync(cancellationToken);
        ApplyResult(result);
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        OperationResult<CompletedSession> result = await timingCoordinator.StopAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error!.Message;
            return;
        }

        pendingSessionId = result.Value!.Id;
        ErrorMessage = null;
        snapshot = null;
        IsActive = false;
        IsPaused = false;
        IsCompletionPending = true;
        StateText = "Describe what you completed";
        UpdateCommandStates();
        RefreshElapsed();
    }

    public async ValueTask ApplyDurationAsync(CancellationToken cancellationToken = default)
    {
        OperationResult<TimingSnapshot> result = await timingCoordinator.ChangeRequestedDurationAsync(DurationText, cancellationToken);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error!.Message;
            return;
        }

        snapshot = result.Value!;
        ApplySnapshot(snapshot.Status == TimingStatus.CompletionPending ? null : snapshot);
        await LoadOldestPendingAsync(cancellationToken);
    }

    public async ValueTask ConfirmTransitionAsync(CancellationToken cancellationToken = default)
    {
        OperationResult<TimingSnapshot> result = snapshot?.Status == TimingStatus.BreakPending
            ? await timingCoordinator.ConfirmBreakAsync(cancellationToken)
            : await timingCoordinator.ConfirmWorkAsync(cancellationToken);
        ApplyResult(result);
    }

    public async ValueTask SkipTransitionAsync(CancellationToken cancellationToken = default)
    {
        OperationResult<TimingSnapshot> result = snapshot?.Status == TimingStatus.ActiveBreak
            ? await timingCoordinator.SkipBreakAsync(cancellationToken)
            : await timingCoordinator.SkipWorkAsync(cancellationToken);
        ApplyResult(result);
    }

    public async ValueTask RefreshAsync(CancellationToken cancellationToken = default)
    {
        TimingSnapshot? latest = await timingCoordinator.GetActiveAsync(cancellationToken);
        if (latest?.Revision != snapshot?.Revision)
        {
            snapshot = latest;
            ApplySnapshot(snapshot);
            if (snapshot is null)
            {
                await LoadOldestPendingAsync(cancellationToken);
            }
        }

        RefreshElapsed();
    }

    public async ValueTask SaveCompletionAsync(CancellationToken cancellationToken = default)
    {
        if (pendingSessionId is not Guid id)
        {
            return;
        }

        OperationResult<bool> result = await timingCoordinator.CompleteDescriptionAsync(
            id,
            CompletionDescription,
            cancellationToken);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error!.Message;
            return;
        }

        pendingSessionId = null;
        CompletionDescription = string.Empty;
        ErrorMessage = null;
        await LoadOldestPendingAsync(cancellationToken);
    }

    public void RefreshElapsed()
    {
        long elapsed = snapshot?.ElapsedWorkAt(timeProvider.GetUtcNow().ToUnixTimeMilliseconds()) ?? 0;
        TimeSpan duration = TimeSpan.FromMilliseconds(elapsed);
        ElapsedText = $"{(long)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
    }

    private void ApplyResult(OperationResult<TimingSnapshot> result)
    {
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error!.Message;
            return;
        }

        ErrorMessage = null;
        snapshot = result.Value;
        ApplySnapshot(snapshot);
    }

    private void ApplySnapshot(TimingSnapshot? value)
    {
        IsActive = value is not null && value.Status != TimingStatus.Paused;
        IsPaused = value?.Status == TimingStatus.Paused;
        StateText = value?.Status switch
        {
            TimingStatus.ActiveWork => "Working",
            TimingStatus.Paused => "Paused",
            TimingStatus.ActiveBreak => "On a break",
            TimingStatus.BreakPending => "Ready for a break",
            TimingStatus.WorkStartPending => "Ready to focus",
            TimingStatus.CompletionPending => "Describe what you completed",
            _ => "Ready to start",
        };
        IsTransitionPending = value?.Status is TimingStatus.BreakPending or TimingStatus.WorkStartPending;
        CanSkipInterval = value?.Mode == TimingMode.Pomodoro && value.Status is TimingStatus.ActiveWork or TimingStatus.ActiveBreak;
        UpdateCommandStates();
        RefreshElapsed();
    }

    private async ValueTask LoadOldestPendingAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<CompletedSession> pending = await timingCoordinator.ListPendingCompletionsAsync(1, cancellationToken);
        pendingSessionId = pending.Count == 0 ? null : pending[0].Id;
        IsCompletionPending = pendingSessionId.HasValue;
        if (IsCompletionPending)
        {
            StateText = "Describe what you completed";
        }
        else if (snapshot is null)
        {
            StateText = "Ready to start";
        }

        allCategories = await organizationStore.ListCategoriesAsync(false, cancellationToken);
        allProjects = await organizationStore.ListProjectsAsync(false, cancellationToken);
        RefreshAssociationOptions();

        UpdateCommandStates();
    }

    public void SelectAssociations(Guid? streamId, IEnumerable<Guid> categoryIds, Guid? projectId)
    {
        SelectedStreamId = streamId;
        SelectedCategoryIds.Clear();
        SelectedCategoryIds.UnionWith(categoryIds);
        SelectedProjectId = projectId;
        RefreshAssociationOptions();
        RefreshCategorySummary();
    }

    public void RefreshCategorySummary()
    {
        string[] names = Categories.Where(item => SelectedCategoryIds.Contains(item.Id)).Select(item => item.Name).ToArray();
        CategorySummary = names.Length switch
        {
            0 => "Choose categories",
            1 => names[0],
            _ => $"{names[0]} +{names.Length - 1}",
        };
    }

    partial void OnSelectedStreamIdChanged(Guid? value) => RefreshAssociationOptions();

    private void RefreshAssociationOptions()
    {
        Categories.Clear();
        foreach (CategoryDefinition category in allCategories.Where(item => item.StreamId is null || item.StreamId == SelectedStreamId))
        {
            Categories.Add(new AssociationOption(category.Id, category.Name));
        }

        Projects.Clear();
        foreach (ProjectDefinition project in allProjects.Where(item => item.StreamId is null || item.StreamId == SelectedStreamId))
        {
            Projects.Add(new AssociationOption(project.Id, project.Name));
        }

        SelectedCategoryIds.RemoveWhere(id => Categories.All(item => item.Id != id));
        if (SelectedProjectId.HasValue && Projects.All(item => item.Id != SelectedProjectId))
        {
            SelectedProjectId = null;
        }

        RefreshCategorySummary();
    }

    private void UpdateCommandStates()
    {
        CanStart = snapshot is null && !IsCompletionPending;
        CanPause = snapshot?.Status is TimingStatus.ActiveWork or TimingStatus.ActiveBreak or TimingStatus.BreakPending;
        CanResume = snapshot?.Status == TimingStatus.Paused;
        CanStop = snapshot is not null;
        CanApplyDuration = snapshot?.Mode == TimingMode.Normal &&
            snapshot.Status is TimingStatus.ActiveWork or TimingStatus.Paused;
    }
}
