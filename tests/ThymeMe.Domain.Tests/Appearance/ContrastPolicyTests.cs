using ThymeMe.Domain.Appearance;
using ThymeMe.Domain.Common;

namespace ThymeMe.Domain.Tests.Appearance;

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

    [Theory]
    [InlineData("")]
    [InlineData(" #FFFFFF")]
    [InlineData("#FFFF")]
    [InlineData("#GGGGGG")]
    public void PaletteVariantRejectsNonStrictColors(string color)
    {
        PaletteVariant variant = new(color, "#FFFFFF", "#000000");

        Assert.Throws<DomainValidationException>(variant.Validate);
    }
}
