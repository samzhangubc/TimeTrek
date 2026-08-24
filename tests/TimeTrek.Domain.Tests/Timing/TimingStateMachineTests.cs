using TimeTrek.Domain.Billing;
using TimeTrek.Domain.Organization;
using TimeTrek.Domain.Reporting;
using TimeTrek.Domain.Timing;

namespace TimeTrek.Domain.Tests.Timing;

public sealed class TimingStateMachineTests
{
    private static readonly BillingSnapshot NonBillable = BillingSnapshot.Create(false, null, null, 0);

    [Fact]
    public void PauseAndResumeExcludePausedTime()
    {
        TimingSnapshot active = TimingStateMachine.Start(
            Guid.CreateVersion7(),
            1_000,
            60_000,
            TimingMode.Normal,
            SessionAssociations.Empty,
            NonBillable,
            null,
            RoundingRule.None);

        TimingSnapshot paused = TimingStateMachine.Pause(active, 11_000);
        TimingSnapshot resumed = TimingStateMachine.ResumeWork(paused, 31_000);
        TimingSnapshot completed = TimingStateMachine.Complete(resumed, 41_000);

        Assert.Equal(20_000, completed.AccumulatedWorkMilliseconds);
        Assert.Equal(TimingStatus.CompletionPending, completed.Status);
    }

    [Fact]
    public void RecoverStopsAtCommittedScheduledEnd()
    {
        TimingSnapshot active = TimingStateMachine.Start(
            Guid.CreateVersion7(),
            10_000,
            25_000,
            TimingMode.Normal,
            SessionAssociations.Empty,
            NonBillable,
            null,
            RoundingRule.None);

        TimingSnapshot recovered = TimingStateMachine.Recover(active, 100_000);

        Assert.Equal(TimingStatus.CompletionPending, recovered.Status);
        Assert.Equal(25_000, recovered.AccumulatedWorkMilliseconds);
    }

    [Fact]
    public void ReducingDurationBelowElapsedCompletesImmediately()
    {
        TimingSnapshot active = TimingStateMachine.Start(
            Guid.CreateVersion7(),
            0,
            60_000,
            TimingMode.Normal,
            SessionAssociations.Empty,
            NonBillable,
            null,
            RoundingRule.None);

        TimingSnapshot changed = TimingStateMachine.ChangeRequestedDuration(active, 20_000, 10_000);

        Assert.Equal(TimingStatus.CompletionPending, changed.Status);
        Assert.Equal(20_000, changed.AccumulatedWorkMilliseconds);
    }
}
