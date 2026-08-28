using System.Globalization;

namespace ThymeMe.Domain.Timing;

public sealed record DurationParseResult(bool IsValid, long Milliseconds, bool RequiresLongDurationWarning, string? Error)
{
    public static DurationParseResult Invalid { get; } = new(
        false,
        0,
        false,
        "Use formats such as 45, 1.5h, 1h 30m, 90m, or 01:30.");
}

public static class DurationParser
{
    public const int MaximumInputLength = 64;
    public const long MaximumDurationMilliseconds = 365L * 24 * 60 * 60 * 1000;
    private const long WarningThresholdMilliseconds = 24L * 60 * 60 * 1000;

    public static DurationParseResult Parse(string? input, CultureInfo? culture = null)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return DurationParseResult.Invalid;
        }

        string text = input.Trim();
        if (text.Length > MaximumInputLength)
        {
            return DurationParseResult.Invalid;
        }

        culture ??= CultureInfo.CurrentCulture;
        decimal minutes;

        if (text.Contains(':', StringComparison.Ordinal))
        {
            if (!TryParseClock(text, out minutes))
            {
                return DurationParseResult.Invalid;
            }
        }
        else if (text.EndsWith("h", StringComparison.OrdinalIgnoreCase) ||
                 text.EndsWith("m", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryParseUnits(text, culture, out minutes))
            {
                return DurationParseResult.Invalid;
            }
        }
        else if (!TryParseDecimal(text, culture, out minutes))
        {
            return DurationParseResult.Invalid;
        }

        decimal millisecondsValue = minutes * 60_000m;
        if (millisecondsValue <= 0 || millisecondsValue > MaximumDurationMilliseconds)
        {
            return DurationParseResult.Invalid;
        }

        long milliseconds;
        try
        {
            milliseconds = checked((long)decimal.Round(millisecondsValue, 0, MidpointRounding.ToEven));
        }
        catch (OverflowException)
        {
            return DurationParseResult.Invalid;
        }

        return new(true, milliseconds, milliseconds >= WarningThresholdMilliseconds, null);
    }

    private static bool TryParseClock(string text, out decimal minutes)
    {
        minutes = 0;
        string[] parts = text.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length != 2 ||
            !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out long hours) ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int minutePart) ||
            hours < 0 || minutePart is < 0 or > 59)
        {
            return false;
        }

        try
        {
            minutes = checked((decimal)hours * 60 + minutePart);
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static bool TryParseUnits(string text, CultureInfo culture, out decimal minutes)
    {
        minutes = 0;
        string normalized = text.Trim();
        int hIndex = normalized.IndexOf('h', StringComparison.OrdinalIgnoreCase);
        int mIndex = normalized.IndexOf('m', StringComparison.OrdinalIgnoreCase);

        if (hIndex >= 0)
        {
            if (!TryParseDecimal(normalized[..hIndex].Trim(), culture, out decimal hours))
            {
                return false;
            }

            minutes = hours * 60;
            string remainder = normalized[(hIndex + 1)..].Trim();
            if (remainder.Length == 0)
            {
                return hIndex == normalized.Length - 1;
            }

            if (!remainder.EndsWith("m", StringComparison.OrdinalIgnoreCase) ||
                !TryParseDecimal(remainder[..^1].Trim(), culture, out decimal minutePart) ||
                minutePart is < 0 or >= 60)
            {
                return false;
            }

            minutes += minutePart;
            return true;
        }

        return mIndex == normalized.Length - 1 &&
               TryParseDecimal(normalized[..^1].Trim(), culture, out minutes);
    }

    private static bool TryParseDecimal(string text, CultureInfo culture, out decimal value)
    {
        const NumberStyles styles = NumberStyles.AllowDecimalPoint;
        return decimal.TryParse(text, styles, culture, out value) ||
               decimal.TryParse(text, styles, CultureInfo.InvariantCulture, out value);
    }
}
