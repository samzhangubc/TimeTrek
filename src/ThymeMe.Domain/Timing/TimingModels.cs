using ThymeMe.Domain.Billing;
using ThymeMe.Domain.Organization;
using ThymeMe.Domain.Reporting;

namespace ThymeMe.Domain.Timing;

public enum SessionOrigin
{
    Tracked,
    Manual,
    Continued,
    Recreated,
    Adjustment,
}

public enum TimingMode
{
    Normal,
    Pomodoro,
}

public enum TimingStatus
{
    ActiveWork,
    Paused,
    BreakPending,
    ActiveBreak,
    WorkStartPending,
    CompletionPending,
}

public enum TimingSegmentKind
{
    Work,
    Break,
    Buffer,
    Pause,
}

public sealed record PomodoroPlan(
    long TotalMilliseconds,
    long WorkMilliseconds,
    long BreakMilliseconds,
    long BufferMilliseconds,
    bool BreaksBillable)
{
    public static PomodoroPlan Create(
        long totalMilliseconds,
        long workMilliseconds,
        long breakMilliseconds,
        bool breaksBillable)
    {
        if (totalMilliseconds <= 0 || workMilliseconds <= 0 || breakMilliseconds <= 0)
        {
            throw new Common.DomainValidationException("Pomodoro durations must be positive.");
        }

        return new(totalMilliseconds, workMilliseconds, breakMilliseconds, 5 * 60_000, breaksBillable);
    }
}

public sealed record TimingSnapshot(
    Guid RootId,
    Guid SessionId,
    TimingStatus Status,
    TimingMode Mode,
    SessionAssociations Associations,
    BillingSnapshot Billing,
    int? RoundingIncrementMinutes,
    RoundingRule RoundingRule,
    long StartedUtcMilliseconds,
    long CurrentSegmentStartedUtcMilliseconds,
    long RequestedDurationMilliseconds,
    long ScheduledEndUtcMilliseconds,
    long AccumulatedWorkMilliseconds,
    long AccumulatedBreakMilliseconds,
    PomodoroPlan? Pomodoro,
    TimingStatus? PausedFromStatus,
    int Revision)
{
    public long ElapsedWorkAt(long nowUtcMilliseconds) => Status is TimingStatus.ActiveWork or TimingStatus.BreakPending
        ? checked(AccumulatedWorkMilliseconds + Math.Max(0, nowUtcMilliseconds - CurrentSegmentStartedUtcMilliseconds))
        : AccumulatedWorkMilliseconds;
}

public sealed record CompletedSession(
    Guid Id,
    SessionOrigin Origin,
    TimingMode Mode,
    SessionAssociations Associations,
    long StartUtcMilliseconds,
    long EndUtcMilliseconds,
    string TimeZoneId,
    int StartUtcOffsetMinutes,
    int EndUtcOffsetMinutes,
    RoundingSnapshot Duration,
    long RequestedDurationMilliseconds,
    BillingSnapshot Billing,
    string? Description,
    bool CompletionPending,
    long CreatedUtcMilliseconds,
    long UpdatedUtcMilliseconds);

public sealed record TimingSegment(
    Guid Id,
    Guid SessionId,
    TimingSegmentKind Kind,
    long StartUtcMilliseconds,
    long EndUtcMilliseconds);

public sealed record NegativeAdjustment(
    Guid Id,
    SessionAssociations Associations,
    long LocalDateUnixDays,
    string TimeZoneId,
    RoundingSnapshot Duration,
    BillingSnapshot Billing,
    string? Description,
    long CreatedUtcMilliseconds);
