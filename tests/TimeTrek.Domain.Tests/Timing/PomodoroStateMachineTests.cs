using TimeTrek.Domain.Billing;
using TimeTrek.Domain.Organization;
using TimeTrek.Domain.Reporting;
using TimeTrek.Domain.Timing;

namespace TimeTrek.Domain.Tests.Timing;

public sealed class PomodoroStateMachineTests
{
    [Fact]
    public void WorkBoundaryUsesBufferThenBreakAndRequiresNextWorkConfirmation()
    {
        PomodoroPlan plan = PomodoroPlan.Create(60 * 60_000, 25 * 60_000, 5 * 60_000, false);
        TimingSnapshot state = TimingStateMachine.Start(
            Guid.CreateVersion7(), 0, plan.TotalMilliseconds, TimingMode.Pomodoro,
            SessionAssociations.Empty, BillingSnapshot.Create(false, null, null, 0), null, RoundingRule.None, plan);

        state = TimingStateMachine.AdvanceAutomatic(state);
        Assert.Equal(TimingStatus.BreakPending, state.Status);
        Assert.Equal(25 * 60_000, state.AccumulatedWorkMilliseconds);

        state = TimingStateMachine.AdvanceAutomatic(state);
        Assert.Equal(TimingStatus.ActiveBreak, state.Status);
        Assert.Equal(30 * 60_000, state.AccumulatedWorkMilliseconds);

        state = TimingStateMachine.AdvanceAutomatic(state);
        Assert.Equal(TimingStatus.WorkStartPending, state.Status);
        Assert.Equal(5 * 60_000, state.AccumulatedBreakMilliseconds);
    }

    [Fact]
    public void ManualPauseFreezesPomodoroHardCap()
    {
        PomodoroPlan plan = PomodoroPlan.Create(30 * 60_000, 25 * 60_000, 5 * 60_000, false);
        TimingSnapshot active = TimingStateMachine.Start(
            Guid.CreateVersion7(), 0, plan.TotalMilliseconds, TimingMode.Pomodoro,
            SessionAssociations.Empty, BillingSnapshot.Create(false, null, null, 0), null, RoundingRule.None, plan);

        TimingSnapshot paused = TimingStateMachine.Pause(active, 10 * 60_000);
        TimingSnapshot resumed = TimingStateMachine.ResumeWork(paused, 20 * 60_000);

        Assert.Equal(35 * 60_000, resumed.ScheduledEndUtcMilliseconds);
    }
}
