using System.Globalization;

namespace ThymeMe.Presentation.WinUI.Common;

public sealed record CurrencyOption(string Code, string Name, int MinorUnitDigits)
{
    public string Label => $"{Name} ({Code})";
}

public static class CurrencyCatalog
{
    private static readonly Lazy<List<CurrencyOption>> Options = new(CreateOptions);

    public static IReadOnlyList<CurrencyOption> All => Options.Value;

    public static string RegionalCode
    {
        get
        {
            try
            {
                return RegionInfo.CurrentRegion.ISOCurrencySymbol;
            }
            catch (ArgumentException)
            {
                return "CAD";
            }
        }
    }

    public static long ToMinorUnits(double amount, CurrencyOption currency)
    {
        if (double.IsNaN(amount) || double.IsInfinity(amount) || amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        decimal multiplier = Pow10(currency.MinorUnitDigits);
        return checked((long)decimal.Round((decimal)amount * multiplier, 0, MidpointRounding.ToEven));
    }

    private static List<CurrencyOption> CreateOptions()
    {
        Dictionary<string, CurrencyOption> byCode = new(StringComparer.Ordinal);
        foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            try
            {
                RegionInfo region = new(culture.Name);
                string code = region.ISOCurrencySymbol.ToUpperInvariant();
                if (code.Length != 3)
                {
                    continue;
                }

                int digits = Math.Clamp(culture.NumberFormat.CurrencyDecimalDigits, 0, 4);
                CurrencyOption option = new(code, region.CurrencyEnglishName, digits);
                if (!byCode.TryGetValue(code, out CurrencyOption? existing) || option.Name.Length < existing.Name.Length)
                {
                    byCode[code] = option;
                }
            }
            catch (ArgumentException)
            {
            }
        }

        Ensure(byCode, new CurrencyOption("CAD", "Canadian dollar", 2));
        Ensure(byCode, new CurrencyOption("USD", "US dollar", 2));
        return byCode.Values
            .OrderBy(item => item.Code == "CAD" ? 0 : item.Code == "USD" ? 1 : 2)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ToList();
    }

    private static void Ensure(Dictionary<string, CurrencyOption> options, CurrencyOption value)
    {
        options.TryAdd(value.Code, value);
    }

    private static decimal Pow10(int exponent)
    {
        decimal result = 1;
        for (int index = 0; index < exponent; index++)
        {
            result *= 10;
        }

        return result;
    }
}
