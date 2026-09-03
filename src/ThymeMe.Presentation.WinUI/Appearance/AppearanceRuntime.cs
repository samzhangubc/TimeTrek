using System.Globalization;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using ThymeMe.Application.Settings;
using ThymeMe.Domain.Appearance;
using Windows.UI;

namespace ThymeMe.Presentation.WinUI.Appearance;

public static class AppearanceRuntime
{
    public static bool ResolveDark(AppearanceMode mode, TimeOnly localTime, bool windowsDark, TimeOnly lightStart, TimeOnly darkStart) =>
        mode switch
        {
            AppearanceMode.Light => false,
            AppearanceMode.Dark => true,
            AppearanceMode.FollowWindows => windowsDark,
            AppearanceMode.Scheduled when darkStart > lightStart => localTime >= darkStart || localTime < lightStart,
            AppearanceMode.Scheduled => localTime >= darkStart && localTime < lightStart,
            _ => windowsDark,
        };

    public static void ApplyResources(ResourceDictionary resources, PaletteVariant variant)
    {
        ArgumentNullException.ThrowIfNull(resources);
        PaletteVariant validated = variant.Validate();
        Color canvas = Parse(validated.Canvas);
        Color surface = Parse(validated.Surface);
        Color accent = Parse(validated.Accent);
        Color text = HighestContrast(canvas);
        Color surfaceText = HighestContrast(surface);
        Color accentText = HighestContrast(accent);
        Color secondaryText = ColorHelper.FromArgb(190, surfaceText.R, surfaceText.G, surfaceText.B);
        Color border = Blend(surfaceText, surface, 0.22);

        SetBrush(resources, "ApplicationPageBackgroundThemeBrush", canvas);
        SetBrush(resources, "SolidBackgroundFillColorBaseBrush", canvas);
        SetBrush(resources, "LayerFillColorDefaultBrush", canvas);
        SetBrush(resources, "CardBackgroundFillColorDefaultBrush", surface);
        SetBrush(resources, "CardBackgroundFillColorSecondaryBrush", surface);
        SetBrush(resources, "ControlFillColorDefaultBrush", surface);
        SetBrush(resources, "ControlFillColorSecondaryBrush", Blend(surfaceText, surface, 0.08));
        SetBrush(resources, "AccentFillColorDefaultBrush", accent);
        SetBrush(resources, "AccentFillColorSecondaryBrush", Blend(accentText, accent, 0.12));
        SetBrush(resources, "AccentTextFillColorPrimaryBrush", accent);
        SetBrush(resources, "TextFillColorPrimaryBrush", text);
        SetBrush(resources, "TextFillColorSecondaryBrush", secondaryText);
        SetBrush(resources, "TextOnAccentFillColorPrimaryBrush", accentText);
        SetBrush(resources, "CardStrokeColorDefaultBrush", border);
        SetBrush(resources, "ControlStrokeColorDefaultBrush", border);
    }

    public static Color Parse(string value)
    {
        string normalized = new PaletteVariant(value, value, value).Validate().Canvas;
        return ColorHelper.FromArgb(
            byte.MaxValue,
            byte.Parse(normalized.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(normalized.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(normalized.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }

    public static Color HighestContrast(Color background)
    {
        string hex = $"#{background.R:X2}{background.G:X2}{background.B:X2}";
        return ContrastPolicy.Ratio("#000000", hex) >= ContrastPolicy.Ratio("#FFFFFF", hex)
            ? Colors.Black
            : Colors.White;
    }

    private static void SetBrush(ResourceDictionary resources, string key, Color color) =>
        resources[key] = new SolidColorBrush(color);

    private static Color Blend(Color foreground, Color background, double foregroundWeight)
    {
        byte BlendChannel(byte first, byte second) =>
            checked((byte)Math.Round((first * foregroundWeight) + (second * (1 - foregroundWeight))));
        return ColorHelper.FromArgb(
            byte.MaxValue,
            BlendChannel(foreground.R, background.R),
            BlendChannel(foreground.G, background.G),
            BlendChannel(foreground.B, background.B));
    }
}
