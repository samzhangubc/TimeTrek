using TimeTrek.Domain.Common;
using TimeTrek.Domain.Organization;

namespace TimeTrek.Domain.Appearance;

public sealed record PaletteVariant(string Canvas, string Surface, string Accent)
{
    public PaletteVariant Validate() => this with
    {
        Canvas = ColorValue.NormalizeOptional(Canvas)!,
        Surface = ColorValue.NormalizeOptional(Surface)!,
        Accent = ColorValue.NormalizeOptional(Accent)!,
    };
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
