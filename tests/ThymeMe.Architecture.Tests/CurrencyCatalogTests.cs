using ThymeMe.Presentation.WinUI.Common;

namespace ThymeMe.Architecture.Tests;

public sealed class CurrencyCatalogTests
{
    [Fact]
    public void CanadianAndUsDollarsArePinnedBeforeAlphabeticalCurrencies()
    {
        IReadOnlyList<CurrencyOption> currencies = CurrencyCatalog.All;

        Assert.Equal("CAD", currencies[0].Code);
        Assert.Equal("USD", currencies[1].Code);
        Assert.True(currencies.Count > 2);
    }

    [Theory]
    [InlineData(12.345, 1234)]
    [InlineData(12.355, 1236)]
    public void ToMinorUnitsUsesBankersRounding(double amount, long expected)
    {
        CurrencyOption currency = new("CAD", "Canadian dollar", 2);

        Assert.Equal(expected, CurrencyCatalog.ToMinorUnits(amount, currency));
    }
}
