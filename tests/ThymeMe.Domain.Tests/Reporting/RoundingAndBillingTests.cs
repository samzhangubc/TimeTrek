using ThymeMe.Domain.Billing;
using ThymeMe.Domain.Reporting;

namespace ThymeMe.Domain.Tests.Reporting;

public sealed class RoundingAndBillingTests
{
    [Fact]
    public void RoundingPreservesRawDurationAndUsesBankersMidpoint()
    {
        RoundingSnapshot down = RoundingPolicy.Apply(150_000, 5, RoundingRule.Nearest);
        RoundingSnapshot up = RoundingPolicy.Apply(450_000, 5, RoundingRule.Nearest);

        Assert.Equal(150_000, down.RawMilliseconds);
        Assert.Equal(0, down.EffectiveMilliseconds);
        Assert.Equal(600_000, up.EffectiveMilliseconds);
    }

    [Fact]
    public void BillingStoresCurrencyMinorUnitEstimate()
    {
        BillingSnapshot billing = BillingSnapshot.Create(true, 2_000, "cad", 1_800_000);

        Assert.Equal("CAD", billing.CurrencyCode);
        Assert.Equal(1_000, billing.EstimatedEarningMinorUnits);
    }
}
