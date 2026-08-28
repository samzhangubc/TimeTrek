using ThymeMe.Domain.Common;
using ThymeMe.Domain.Organization;

namespace ThymeMe.Domain.Tests.Organization;

public sealed class TimeBudgetTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidateRejectsNonPositiveDurations(long durationMilliseconds)
    {
        TimeBudget budget = new(durationMilliseconds, BudgetResetPeriod.None);

        _ = Assert.Throws<DomainValidationException>(budget.Validate);
    }

    [Theory]
    [InlineData(BudgetResetPeriod.None)]
    [InlineData(BudgetResetPeriod.Weekly)]
    [InlineData(BudgetResetPeriod.Monthly)]
    public void ValidatePreservesSupportedResetPeriod(BudgetResetPeriod resetPeriod)
    {
        TimeBudget budget = new(60_000, resetPeriod);

        Assert.Same(budget, budget.Validate());
    }
}
