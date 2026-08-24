using TimeTrek.Domain.Common;

namespace TimeTrek.Domain.Appearance;

public static class ContrastPolicy
{
    public const double MinimumNormalTextRatio = 4.5;
    public const double MinimumLargeTextRatio = 3.0;

    public static double Ratio(string first, string second)
    {
        (byte r1, byte g1, byte b1) = Parse(first);
        (byte r2, byte g2, byte b2) = Parse(second);
        double l1 = Luminance(r1, g1, b1);
        double l2 = Luminance(r2, g2, b2);
        return (Math.Max(l1, l2) + 0.05) / (Math.Min(l1, l2) + 0.05);
    }

    public static bool MeetsNormalText(string foreground, string background) =>
        Ratio(foreground, background) >= MinimumNormalTextRatio;

    private static (byte Red, byte Green, byte Blue) Parse(string value)
    {
        string normalized = Organization.ColorValue.NormalizeOptional(value)
            ?? throw new DomainValidationException("A contrast color is required.");
        return (
            byte.Parse(normalized.AsSpan(1, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture),
            byte.Parse(normalized.AsSpan(3, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture),
            byte.Parse(normalized.AsSpan(5, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture));
    }

    private static double Luminance(byte red, byte green, byte blue) =>
        0.2126 * Linear(red) + 0.7152 * Linear(green) + 0.0722 * Linear(blue);

    private static double Linear(byte value)
    {
        double channel = value / 255d;
        return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }
}
