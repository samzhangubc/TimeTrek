using TimeTrek.Domain.Appearance;

namespace TimeTrek.Domain.Tests.Appearance;

public sealed class ContrastPolicyTests
{
    [Fact]
    public void BlackAndWhiteExceedNormalTextContrast()
    {
        Assert.True(ContrastPolicy.MeetsNormalText("#000000", "#FFFFFF"));
        Assert.Equal(21, ContrastPolicy.Ratio("#000000", "#FFFFFF"), 8);
    }

    [Fact]
    public void SimilarColorsFailNormalTextContrast()
    {
        Assert.False(ContrastPolicy.MeetsNormalText("#777777", "#888888"));
    }
}
