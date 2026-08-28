using System.Globalization;
using ThymeMe.Domain.Timing;

namespace ThymeMe.Domain.Tests.Timing;

public sealed class DurationParserTests
{
    [Theory]
    [InlineData("45", 2_700_000)]
    [InlineData("1h", 3_600_000)]
    [InlineData("1.5h", 5_400_000)]
    [InlineData("1h 30m", 5_400_000)]
    [InlineData("90m", 5_400_000)]
    [InlineData("01:30", 5_400_000)]
    public void ParseAcceptsDocumentedFormats(string input, long expectedMilliseconds)
    {
        DurationParseResult result = DurationParser.Parse(input, CultureInfo.InvariantCulture);

        Assert.True(result.IsValid);
        Assert.Equal(expectedMilliseconds, result.Milliseconds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1:60")]
    [InlineData("1h nope")]
    public void ParseRejectsInvalidInput(string? input)
    {
        Assert.False(DurationParser.Parse(input, CultureInfo.InvariantCulture).IsValid);
    }

    [Fact]
    public void ParseAcceptsRegionalDecimalSeparator()
    {
        CultureInfo culture = CultureInfo.GetCultureInfo("fr-FR");

        DurationParseResult result = DurationParser.Parse("1,5h", culture);

        Assert.True(result.IsValid);
        Assert.Equal(5_400_000, result.Milliseconds);
    }
}
