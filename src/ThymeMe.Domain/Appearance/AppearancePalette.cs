using ThymeMe.Domain.Common;
using ThymeMe.Domain.Organization;

namespace ThymeMe.Domain.Appearance;

public sealed record PaletteVariant(string Canvas, string Surface, string Accent)
{
    public PaletteVariant Validate() => this with
    {
        Canvas = RequiredColor(Canvas),
        Surface = RequiredColor(Surface),
        Accent = RequiredColor(Accent),
    };

    private static string RequiredColor(string? value)
    {
        if (value is null || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw new DomainValidationException("Palette colors must use exact #RRGGBB format.");
        }

        return ColorValue.NormalizeOptional(value)
            ?? throw new DomainValidationException("A palette color is required.");
    }
}

public sealed record AppearancePalette(
    Guid Id,
    string Name,
    int FormatVersion,
    bool IsBuiltIn,
    PaletteVariant Light,
    PaletteVariant Dark)
{
    public const int CurrentFormatVersion = 1;

    public AppearancePalette Validate()
    {
        if (FormatVersion != CurrentFormatVersion)
        {
            throw new DomainValidationException("The palette format version is unsupported.");
        }

        return this with
        {
            Name = DomainText.RequiredName(Name, nameof(Name)),
            Light = Light.Validate(),
            Dark = Dark.Validate(),
        };
    }
}
