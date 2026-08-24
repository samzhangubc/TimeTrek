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

public sealed partial class TransportViewModel(
    TimingCoordinator timingCoordinator,
    IOrganizationStore organizationStore,
    IAppSettingsStore settingsStore,
    TimeProvider timeProvider) : ObservableObject
{
    public ObservableCollection<StreamOption> Streams { get; } = [];

    [ObservableProperty]
    public partial Guid? SelectedStreamId { get; set; }

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
            SessionAssociations.Create(SelectedStreamId, null, null),
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
        snapshot = null;
        IsActive = false;
        IsPaused = false;
        IsCompletionPending = true;
        StateText = "Describe what you completed";
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
        StateText = value?.Status.ToString() ?? "Ready";
        IsTransitionPending = value?.Status is TimingStatus.BreakPending or TimingStatus.WorkStartPending;
        CanSkipInterval = value?.Mode == TimingMode.Pomodoro && value.Status is TimingStatus.ActiveWork or TimingStatus.ActiveBreak;
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
            StateText = "Ready";
        }
    }
}
