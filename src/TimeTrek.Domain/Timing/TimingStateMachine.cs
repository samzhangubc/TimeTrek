using TimeTrek.Domain.Billing;
using TimeTrek.Domain.Common;
using TimeTrek.Domain.Organization;
using TimeTrek.Domain.Reporting;

namespace TimeTrek.Domain.Timing;

public static class TimingStateMachine
{
    public static TimingSnapshot Start(
        Guid sessionId,
        long nowUtcMilliseconds,
        long requestedDurationMilliseconds,
        TimingMode mode,
        SessionAssociations associations,
        BillingSnapshot billing,
        int? roundingIncrementMinutes,
        RoundingRule roundingRule,
        PomodoroPlan? pomodoro = null)
    {
        if (requestedDurationMilliseconds <= 0 || requestedDurationMilliseconds > DurationParser.MaximumDurationMilliseconds)
        {
            throw new DomainValidationException("Requested duration is outside the supported range.");
        }

        if ((mode == TimingMode.Pomodoro) != (pomodoro is not null))
        {
            throw new DomainValidationException("Pomodoro mode requires a Pomodoro plan and normal mode cannot use one.");
        }

        long firstInterval = mode == TimingMode.Pomodoro
            ? Math.Min(pomodoro!.WorkMilliseconds, pomodoro.TotalMilliseconds)
            : requestedDurationMilliseconds;

        return new(
            Guid.CreateVersion7(),
            sessionId,
            TimingStatus.ActiveWork,
            mode,
            associations,
            billing,
            roundingIncrementMinutes,
            roundingRule,
            nowUtcMilliseconds,
            nowUtcMilliseconds,
            requestedDurationMilliseconds,
            checked(nowUtcMilliseconds + firstInterval),
            0,
            0,
            pomodoro,
            null,
            1);
    }

    public static TimingSnapshot Pause(TimingSnapshot snapshot, long nowUtcMilliseconds)
    {
        EnsureState(snapshot, TimingStatus.ActiveWork, TimingStatus.ActiveBreak, TimingStatus.BreakPending);
        long elapsed = ElapsedCurrent(snapshot, nowUtcMilliseconds);
        return snapshot with
        {
            Status = TimingStatus.Paused,
            AccumulatedWorkMilliseconds = snapshot.Status == TimingStatus.ActiveBreak
                ? snapshot.AccumulatedWorkMilliseconds
                : checked(snapshot.AccumulatedWorkMilliseconds + elapsed),
            AccumulatedBreakMilliseconds = snapshot.Status == TimingStatus.ActiveBreak
                ? checked(snapshot.AccumulatedBreakMilliseconds + elapsed)
                : snapshot.AccumulatedBreakMilliseconds,
            CurrentSegmentStartedUtcMilliseconds = nowUtcMilliseconds,
            ScheduledEndUtcMilliseconds = 0,
            PausedFromStatus = snapshot.Status,
            Revision = checked(snapshot.Revision + 1),
        };
    }

    public static TimingSnapshot ResumeWork(TimingSnapshot snapshot, long nowUtcMilliseconds)
    {
        EnsureState(snapshot, TimingStatus.Paused, TimingStatus.WorkStartPending);
        TimingStatus target = snapshot.Status == TimingStatus.WorkStartPending
            ? TimingStatus.ActiveWork
            : snapshot.PausedFromStatus ?? TimingStatus.ActiveWork;
        long remaining = RemainingInterval(snapshot, target);

        if (remaining <= 0)
        {
            return Complete(snapshot, nowUtcMilliseconds);
        }

        return snapshot with
        {
            Status = target,
            CurrentSegmentStartedUtcMilliseconds = nowUtcMilliseconds,
            ScheduledEndUtcMilliseconds = checked(nowUtcMilliseconds + remaining),
            PausedFromStatus = null,
            Revision = checked(snapshot.Revision + 1),
        };
    }

    public static TimingSnapshot ChangeRequestedDuration(
        TimingSnapshot snapshot,
        long nowUtcMilliseconds,
        long requestedDurationMilliseconds)
    {
        if (snapshot.Mode != TimingMode.Normal)
        {
            throw new DomainValidationException("Requested duration can be edited only for a normal Session.");
        }

        if (requestedDurationMilliseconds <= 0 || requestedDurationMilliseconds > DurationParser.MaximumDurationMilliseconds)
        {
            throw new DomainValidationException("Requested duration is outside the supported range.");
        }

        long elapsed = snapshot.ElapsedWorkAt(nowUtcMilliseconds);
        if (requestedDurationMilliseconds <= elapsed)
        {
            return Complete(snapshot with { RequestedDurationMilliseconds = requestedDurationMilliseconds }, nowUtcMilliseconds);
        }

        return snapshot with
        {
            RequestedDurationMilliseconds = requestedDurationMilliseconds,
            ScheduledEndUtcMilliseconds = snapshot.Status == TimingStatus.ActiveWork
                ? checked(nowUtcMilliseconds + requestedDurationMilliseconds - elapsed)
                : 0,
            Revision = checked(snapshot.Revision + 1),
        };
    }

    public static TimingSnapshot Complete(TimingSnapshot snapshot, long nowUtcMilliseconds)
    {
        if (snapshot.Status == TimingStatus.CompletionPending)
        {
            return snapshot;
        }

        long work = snapshot.AccumulatedWorkMilliseconds;
        long breaks = snapshot.AccumulatedBreakMilliseconds;
        if (snapshot.Status is TimingStatus.ActiveWork or TimingStatus.BreakPending)
        {
            work = checked(work + ElapsedCurrent(snapshot, nowUtcMilliseconds));
        }
        else if (snapshot.Status == TimingStatus.ActiveBreak)
        {
            breaks = checked(breaks + ElapsedCurrent(snapshot, nowUtcMilliseconds));
        }

        return snapshot with
        {
            Status = TimingStatus.CompletionPending,
            AccumulatedWorkMilliseconds = work,
            AccumulatedBreakMilliseconds = breaks,
            CurrentSegmentStartedUtcMilliseconds = nowUtcMilliseconds,
            ScheduledEndUtcMilliseconds = 0,
            Revision = checked(snapshot.Revision + 1),
        };
    }

    public static TimingSnapshot Recover(TimingSnapshot snapshot, long nowUtcMilliseconds)
    {
        TimingSnapshot current = snapshot;
        int guard = 0;
        while (current.ScheduledEndUtcMilliseconds > 0 &&
               current.ScheduledEndUtcMilliseconds <= nowUtcMilliseconds &&
               current.Status != TimingStatus.CompletionPending &&
               guard++ < 1000)
        {
            current = AdvanceAutomatic(current);
        }

        return current;
    }

    public static TimingSnapshot AdvanceAutomatic(TimingSnapshot snapshot)
    {
        if (snapshot.ScheduledEndUtcMilliseconds <= 0)
        {
            throw new DomainValidationException("This timing state has no automatic boundary.");
        }

        long boundary = snapshot.ScheduledEndUtcMilliseconds;
        if (snapshot.Mode == TimingMode.Normal)
        {
            return Complete(snapshot, boundary);
        }

        PomodoroPlan plan = snapshot.Pomodoro!;
        return snapshot.Status switch
        {
            TimingStatus.ActiveWork => BeginBreakPending(snapshot, boundary, plan),
            TimingStatus.BreakPending => BeginBreak(snapshot, boundary, plan),
            TimingStatus.ActiveBreak => FinishBreak(snapshot, boundary),
            _ => throw new DomainValidationException($"The {snapshot.Status} state has no automatic Pomodoro transition."),
        };
    }

    public static TimingSnapshot ConfirmBreak(TimingSnapshot snapshot, long nowUtcMilliseconds)
    {
        EnsureState(snapshot, TimingStatus.BreakPending);
        return BeginBreak(snapshot, nowUtcMilliseconds, snapshot.Pomodoro!);
    }

    public static TimingSnapshot ConfirmWork(TimingSnapshot snapshot, long nowUtcMilliseconds) =>
        ResumeWork(snapshot, nowUtcMilliseconds);

    public static TimingSnapshot SkipWork(TimingSnapshot snapshot, long nowUtcMilliseconds)
    {
        EnsureState(snapshot, TimingStatus.ActiveWork, TimingStatus.BreakPending);
        TimingSnapshot pending = snapshot.Status == TimingStatus.ActiveWork
            ? BeginBreakPending(snapshot, nowUtcMilliseconds, snapshot.Pomodoro!)
            : snapshot;
        return BeginBreak(pending, nowUtcMilliseconds, snapshot.Pomodoro!);
    }

    public static TimingSnapshot SkipBreak(TimingSnapshot snapshot, long nowUtcMilliseconds)
    {
        EnsureState(snapshot, TimingStatus.ActiveBreak);
        long elapsed = ElapsedCurrent(snapshot, nowUtcMilliseconds);
        return snapshot with
        {
            Status = TimingStatus.WorkStartPending,
            AccumulatedBreakMilliseconds = checked(snapshot.AccumulatedBreakMilliseconds + elapsed),
            CurrentSegmentStartedUtcMilliseconds = nowUtcMilliseconds,
            ScheduledEndUtcMilliseconds = 0,
            Revision = checked(snapshot.Revision + 1),
        };
    }

    private static long CycleElapsed(TimingSnapshot snapshot) =>
        checked(snapshot.AccumulatedWorkMilliseconds + snapshot.AccumulatedBreakMilliseconds);

    private static long RemainingInterval(TimingSnapshot snapshot, TimingStatus target)
    {
        if (snapshot.Mode == TimingMode.Normal)
        {
            return snapshot.RequestedDurationMilliseconds - snapshot.AccumulatedWorkMilliseconds;
        }

        PomodoroPlan plan = snapshot.Pomodoro!;
        long totalRemaining = plan.TotalMilliseconds - CycleElapsed(snapshot);
        long intervalRemaining = target switch
        {
            TimingStatus.ActiveBreak => plan.BreakMilliseconds - snapshot.AccumulatedBreakMilliseconds % plan.BreakMilliseconds,
            TimingStatus.BreakPending => plan.BufferMilliseconds - snapshot.AccumulatedWorkMilliseconds % (plan.WorkMilliseconds + plan.BufferMilliseconds),
            _ when snapshot.Status == TimingStatus.WorkStartPending => plan.WorkMilliseconds,
            _ => plan.WorkMilliseconds - snapshot.AccumulatedWorkMilliseconds % (plan.WorkMilliseconds + plan.BufferMilliseconds),
        };
        return Math.Min(intervalRemaining, totalRemaining);
    }

    private static TimingSnapshot BeginBreakPending(TimingSnapshot snapshot, long boundary, PomodoroPlan plan)
    {
        long work = checked(snapshot.AccumulatedWorkMilliseconds + ElapsedCurrent(snapshot, boundary));
        if (work + snapshot.AccumulatedBreakMilliseconds >= plan.TotalMilliseconds)
        {
            return Complete(snapshot with { AccumulatedWorkMilliseconds = work, CurrentSegmentStartedUtcMilliseconds = boundary }, boundary);
        }

        long buffer = Math.Min(plan.BufferMilliseconds, plan.TotalMilliseconds - work - snapshot.AccumulatedBreakMilliseconds);
        return snapshot with
        {
            Status = TimingStatus.BreakPending,
            AccumulatedWorkMilliseconds = work,
            CurrentSegmentStartedUtcMilliseconds = boundary,
            ScheduledEndUtcMilliseconds = checked(boundary + buffer),
            Revision = checked(snapshot.Revision + 1),
        };
    }

    private static TimingSnapshot BeginBreak(TimingSnapshot snapshot, long boundary, PomodoroPlan plan)
    {
        long bufferWork = snapshot.Status == TimingStatus.BreakPending
            ? ElapsedCurrent(snapshot, boundary)
            : 0;
        long work = checked(snapshot.AccumulatedWorkMilliseconds + bufferWork);
        long elapsed = checked(work + snapshot.AccumulatedBreakMilliseconds);
        if (elapsed >= plan.TotalMilliseconds)
        {
            return Complete(snapshot with { AccumulatedWorkMilliseconds = work, CurrentSegmentStartedUtcMilliseconds = boundary }, boundary);
        }

        long breakDuration = Math.Min(plan.BreakMilliseconds, plan.TotalMilliseconds - elapsed);
        return snapshot with
        {
            Status = TimingStatus.ActiveBreak,
            AccumulatedWorkMilliseconds = work,
            CurrentSegmentStartedUtcMilliseconds = boundary,
            ScheduledEndUtcMilliseconds = checked(boundary + breakDuration),
            Revision = checked(snapshot.Revision + 1),
        };
    }

    private static TimingSnapshot FinishBreak(TimingSnapshot snapshot, long boundary)
    {
        long breaks = checked(snapshot.AccumulatedBreakMilliseconds + ElapsedCurrent(snapshot, boundary));
        if (CycleElapsed(snapshot with { AccumulatedBreakMilliseconds = breaks }) >= snapshot.Pomodoro!.TotalMilliseconds)
        {
            return Complete(snapshot with { AccumulatedBreakMilliseconds = breaks, CurrentSegmentStartedUtcMilliseconds = boundary }, boundary);
        }

        return snapshot with
        {
            Status = TimingStatus.WorkStartPending,
            AccumulatedBreakMilliseconds = breaks,
            CurrentSegmentStartedUtcMilliseconds = boundary,
            ScheduledEndUtcMilliseconds = 0,
            Revision = checked(snapshot.Revision + 1),
        };
    }

    private static long ElapsedCurrent(TimingSnapshot snapshot, long nowUtcMilliseconds)
    {
        if (nowUtcMilliseconds < snapshot.CurrentSegmentStartedUtcMilliseconds)
        {
            throw new DomainValidationException("A timing transition cannot precede the current segment.");
        }

        return checked(nowUtcMilliseconds - snapshot.CurrentSegmentStartedUtcMilliseconds);
    }

    private static void EnsureState(TimingSnapshot snapshot, params TimingStatus[] allowed)
    {
        if (!allowed.Contains(snapshot.Status))
        {
            throw new DomainValidationException($"The {snapshot.Status} timing state does not allow this transition.");
        }
    }
}
