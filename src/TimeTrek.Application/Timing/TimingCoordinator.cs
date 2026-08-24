using TimeTrek.Application.Common;
using TimeTrek.Application.Organization;
using TimeTrek.Domain.Billing;
using TimeTrek.Domain.Common;
using TimeTrek.Domain.Organization;
using TimeTrek.Domain.Reporting;
using TimeTrek.Domain.Timing;

namespace TimeTrek.Application.Timing;

public sealed class TimingCoordinator(
    ITimingStore store,
    IOrganizationStore organizationStore,
    ILocalTimeContext localTime,
    TimeProvider timeProvider) : IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool disposed;

    public ValueTask<TimingSnapshot?> GetActiveAsync(CancellationToken cancellationToken = default) =>
        store.GetActiveAsync(cancellationToken);

    public ValueTask<IReadOnlyList<CompletedSession>> ListPendingCompletionsAsync(
        int limit = 200,
        CancellationToken cancellationToken = default) =>
        store.ListPendingCompletionsAsync(limit, cancellationToken);

    public async ValueTask<OperationResult<TimingCommandState>> StartAsync(
        StartTimingRequest request,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            DurationParseResult duration = DurationParser.Parse(request.Duration);
            if (!duration.IsValid)
            {
                return OperationResult.Failure<TimingCommandState>("timing.duration", duration.Error!);
            }

            AssociationContext context = await organizationStore
                .GetAssociationContextAsync(request.Associations, cancellationToken)
                .ConfigureAwait(false);
            AssociationPolicy.Validate(request.Associations, context.Categories, context.Projects);

            long now = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
            BillingSnapshot billing = BillingSnapshot.Create(
                request.IsBillable,
                request.HourlyRateMinorUnits,
                request.CurrencyCode,
                0);
            TimingSnapshot snapshot = TimingStateMachine.Start(
                Guid.CreateVersion7(),
                now,
                duration.Milliseconds,
                request.Mode,
                request.Associations,
                billing,
                request.RoundingIncrementMinutes,
                request.RoundingRule,
                request.Pomodoro);

            if (!await store.TryStartAsync(snapshot, cancellationToken).ConfigureAwait(false))
            {
                return OperationResult.Failure<TimingCommandState>(
                    "timing.active_exists",
                    "Only one Session can be active at a time.");
            }

            return OperationResult.Success(
                new TimingCommandState(snapshot, duration.RequiresLongDurationWarning));
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure<TimingCommandState>("timing.invalid", exception.Message);
        }
        finally
        {
            gate.Release();
        }
    }

    public ValueTask<OperationResult<TimingSnapshot>> PauseAsync(CancellationToken cancellationToken = default) =>
        TransitionAsync(TimingStateMachine.Pause, cancellationToken);

    public ValueTask<OperationResult<TimingSnapshot>> ResumeAsync(CancellationToken cancellationToken = default) =>
        TransitionAsync(TimingStateMachine.ResumeWork, cancellationToken);

    public async ValueTask<OperationResult<TimingSnapshot>> ChangeRequestedDurationAsync(
        string durationText,
        CancellationToken cancellationToken = default)
    {
        DurationParseResult parsed = DurationParser.Parse(durationText);
        if (!parsed.IsValid)
        {
            return OperationResult.Failure<TimingSnapshot>("timing.duration", parsed.Error!);
        }

        ObjectDisposedException.ThrowIf(disposed, this);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            TimingSnapshot? current = await store.GetActiveAsync(cancellationToken).ConfigureAwait(false);
            if (current is null)
            {
                return OperationResult.Failure<TimingSnapshot>("timing.inactive", "No Session is active.");
            }

            long now = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
            TimingSnapshot next = TimingStateMachine.ChangeRequestedDuration(current, now, parsed.Milliseconds);
            bool saved = next.Status == TimingStatus.CompletionPending
                ? await store.TryFinalizeAsync(next, current.Revision, BuildCompletedSession(next, now), cancellationToken).ConfigureAwait(false)
                : await store.TryTransitionAsync(next, current.Revision, cancellationToken).ConfigureAwait(false);
            return saved
                ? OperationResult.Success(next)
                : OperationResult.Failure<TimingSnapshot>("timing.conflict", "The Session changed concurrently.");
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure<TimingSnapshot>("timing.duration", exception.Message);
        }
        finally
        {
            gate.Release();
        }
    }

    public ValueTask<OperationResult<TimingSnapshot>> ConfirmBreakAsync(CancellationToken cancellationToken = default) =>
        TransitionAsync(TimingStateMachine.ConfirmBreak, cancellationToken);

    public ValueTask<OperationResult<TimingSnapshot>> ConfirmWorkAsync(CancellationToken cancellationToken = default) =>
        TransitionAsync(TimingStateMachine.ConfirmWork, cancellationToken);

    public ValueTask<OperationResult<TimingSnapshot>> SkipWorkAsync(CancellationToken cancellationToken = default) =>
        TransitionAsync(TimingStateMachine.SkipWork, cancellationToken);

    public ValueTask<OperationResult<TimingSnapshot>> SkipBreakAsync(CancellationToken cancellationToken = default) =>
        TransitionAsync(TimingStateMachine.SkipBreak, cancellationToken);

    public async ValueTask<OperationResult<CompletedSession>> StopAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            TimingSnapshot? current = await store.GetActiveAsync(cancellationToken).ConfigureAwait(false);
            if (current is null)
            {
                return OperationResult.Failure<CompletedSession>("timing.inactive", "No Session is active.");
            }

            long now = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
            TimingSnapshot completed = TimingStateMachine.Complete(current, now);
            CompletedSession session = BuildCompletedSession(completed, now);

            if (!await store.TryFinalizeAsync(completed, current.Revision, session, cancellationToken).ConfigureAwait(false))
            {
                return OperationResult.Failure<CompletedSession>(
                    "timing.conflict",
                    "The Session changed before Stop could be saved. Refresh and try again.");
            }

            return OperationResult.Success(session);
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure<CompletedSession>("timing.invalid", exception.Message);
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask<OperationResult<TimingSnapshot>> RecoverAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            TimingSnapshot? current = await store.GetActiveAsync(cancellationToken).ConfigureAwait(false);
            if (current is null)
            {
                return OperationResult.Failure<TimingSnapshot>("timing.inactive", "No Session requires recovery.");
            }

            TimingSnapshot recovered = TimingStateMachine.Recover(
                current,
                timeProvider.GetUtcNow().ToUnixTimeMilliseconds());
            if (recovered == current)
            {
                return OperationResult.Success(current);
            }

            bool saved = recovered.Status == TimingStatus.CompletionPending
                ? await store.TryFinalizeAsync(
                    recovered,
                    current.Revision,
                    BuildCompletedSession(recovered, recovered.CurrentSegmentStartedUtcMilliseconds),
                    cancellationToken).ConfigureAwait(false)
                : await store.TryTransitionAsync(recovered, current.Revision, cancellationToken).ConfigureAwait(false);
            if (!saved)
            {
                return OperationResult.Failure<TimingSnapshot>("timing.conflict", "Recovery state changed concurrently.");
            }

            return OperationResult.Success(recovered);
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask<OperationResult<TimingSnapshot>> AdvanceAutomaticAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            TimingSnapshot? current = await store.GetActiveAsync(cancellationToken).ConfigureAwait(false);
            long now = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
            if (current is null || current.ScheduledEndUtcMilliseconds <= 0 || current.ScheduledEndUtcMilliseconds > now)
            {
                return current is null
                    ? OperationResult.Failure<TimingSnapshot>("timing.inactive", "No Session is active.")
                    : OperationResult.Success(current);
            }

            TimingSnapshot next = TimingStateMachine.AdvanceAutomatic(current);
            bool saved;
            if (next.Status == TimingStatus.CompletionPending)
            {
                long end = next.CurrentSegmentStartedUtcMilliseconds;
                saved = await store.TryFinalizeAsync(
                    next,
                    current.Revision,
                    BuildCompletedSession(next, end),
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                saved = await store.TryTransitionAsync(next, current.Revision, cancellationToken).ConfigureAwait(false);
            }

            return saved
                ? OperationResult.Success(next)
                : OperationResult.Failure<TimingSnapshot>("timing.conflict", "The automatic transition changed concurrently.");
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure<TimingSnapshot>("timing.automatic", exception.Message);
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask<OperationResult<bool>> CompleteDescriptionAsync(
        Guid sessionId,
        string? description,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string? validated = DomainText.OptionalDescription(description);
            await store.CompleteDescriptionAsync(
                sessionId,
                validated,
                timeProvider.GetUtcNow().ToUnixTimeMilliseconds(),
                cancellationToken).ConfigureAwait(false);
            return OperationResult.Success(true);
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure<bool>("completion.invalid", exception.Message);
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        gate.Dispose();
        disposed = true;
    }

    private async ValueTask<OperationResult<TimingSnapshot>> TransitionAsync(
        Func<TimingSnapshot, long, TimingSnapshot> transition,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            TimingSnapshot? current = await store.GetActiveAsync(cancellationToken).ConfigureAwait(false);
            if (current is null)
            {
                return OperationResult.Failure<TimingSnapshot>("timing.inactive", "No Session is active.");
            }

            TimingSnapshot next = transition(current, timeProvider.GetUtcNow().ToUnixTimeMilliseconds());
            if (!await store.TryTransitionAsync(next, current.Revision, cancellationToken).ConfigureAwait(false))
            {
                return OperationResult.Failure<TimingSnapshot>("timing.conflict", "The Session changed concurrently.");
            }

            return OperationResult.Success(next);
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure<TimingSnapshot>("timing.invalid_transition", exception.Message);
        }
        finally
        {
            gate.Release();
        }
    }

    private CompletedSession BuildCompletedSession(TimingSnapshot completed, long endUtcMilliseconds)
    {
        RoundingSnapshot duration = RoundingPolicy.Apply(
            completed.AccumulatedWorkMilliseconds,
            completed.RoundingIncrementMinutes,
            completed.RoundingRule);
        BillingSnapshot billing = BillingSnapshot.Create(
            completed.Billing.IsBillable,
            completed.Billing.HourlyRateMinorUnits,
            completed.Billing.CurrencyCode,
            duration.EffectiveMilliseconds);
        return new(
            completed.SessionId,
            SessionOrigin.Tracked,
            completed.Mode,
            completed.Associations,
            completed.StartedUtcMilliseconds,
            endUtcMilliseconds,
            localTime.TimeZoneId,
            localTime.GetUtcOffsetMinutes(completed.StartedUtcMilliseconds),
            localTime.GetUtcOffsetMinutes(endUtcMilliseconds),
            duration,
            completed.RequestedDurationMilliseconds,
            billing,
            null,
            true,
            completed.StartedUtcMilliseconds,
            endUtcMilliseconds);
    }
}
