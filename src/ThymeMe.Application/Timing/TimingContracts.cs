using ThymeMe.Domain.Billing;
using ThymeMe.Domain.Organization;
using ThymeMe.Domain.Reporting;
using ThymeMe.Domain.Timing;

namespace ThymeMe.Application.Timing;

public interface ITimingStore
{
    ValueTask<TimingSnapshot?> GetActiveAsync(CancellationToken cancellationToken = default);

    ValueTask<bool> TryStartAsync(TimingSnapshot snapshot, CancellationToken cancellationToken = default);

    ValueTask<bool> TryTransitionAsync(TimingSnapshot snapshot, int expectedRevision, CancellationToken cancellationToken = default);

    ValueTask<bool> TryFinalizeAsync(
        TimingSnapshot snapshot,
        int expectedRevision,
        CompletedSession session,
        CancellationToken cancellationToken = default);

    ValueTask CompleteDescriptionAsync(Guid sessionId, string? description, long updatedUtcMilliseconds, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<CompletedSession>> ListPendingCompletionsAsync(int limit, CancellationToken cancellationToken = default);
}

public interface ILocalTimeContext
{
    string TimeZoneId { get; }

    int GetUtcOffsetMinutes(long utcMilliseconds);
}

public sealed record StartTimingRequest(
    string Duration,
    TimingMode Mode,
    SessionAssociations Associations,
    bool IsBillable,
    long? HourlyRateMinorUnits,
    string? CurrencyCode,
    int? RoundingIncrementMinutes,
    RoundingRule RoundingRule,
    PomodoroPlan? Pomodoro);

public sealed record TimingCommandState(TimingSnapshot Snapshot, bool RequiresLongDurationWarning);
