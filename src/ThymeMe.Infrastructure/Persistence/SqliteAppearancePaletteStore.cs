using Microsoft.EntityFrameworkCore;
using ThymeMe.Application.Appearance;
using ThymeMe.Domain.Appearance;

namespace ThymeMe.Infrastructure.Persistence;

internal sealed class SqliteAppearancePaletteStore(IDbContextFactory<ThymeMeDbContext> contextFactory) : IAppearancePaletteStore
{
    public async ValueTask<IReadOnlyList<AppearancePalette>> ListCustomAsync(CancellationToken cancellationToken = default)
    {
        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        List<PaletteRow> rows = await context.Palettes.AsNoTracking()
            .Where(item => !item.IsBuiltIn)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(ToDomain).ToList();
    }

    public async ValueTask AddAsync(AppearancePalette palette, CancellationToken cancellationToken = default)
    {
        AppearancePalette validated = palette.Validate();
        if (validated.IsBuiltIn)
        {
            throw new InvalidOperationException("Built-in palettes cannot be stored as custom palettes.");
        }

        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        if (await context.Palettes.AnyAsync(item => item.Id == validated.Id, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("A palette with this identifier already exists.");
        }

        context.Palettes.Add(ToRow(validated));
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask UpdateAsync(AppearancePalette palette, CancellationToken cancellationToken = default)
    {
        AppearancePalette validated = palette.Validate();
        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        PaletteRow row = await context.Palettes.SingleOrDefaultAsync(item => item.Id == validated.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The custom palette no longer exists.");
        if (row.IsBuiltIn)
        {
            throw new InvalidOperationException("Built-in palettes cannot be edited.");
        }

        row.Name = validated.Name;
        row.FormatVersion = validated.FormatVersion;
        row.LightCanvas = validated.Light.Canvas;
        row.LightSurface = validated.Light.Surface;
        row.LightAccent = validated.Light.Accent;
        row.DarkCanvas = validated.Dark.Canvas;
        row.DarkSurface = validated.Dark.Surface;
        row.DarkAccent = validated.Dark.Accent;
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        PaletteRow row = await context.Palettes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The custom palette no longer exists.");
        if (row.IsBuiltIn)
        {
            throw new InvalidOperationException("Built-in palettes cannot be deleted.");
        }

        context.Palettes.Remove(row);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static AppearancePalette ToDomain(PaletteRow row) => new(
        row.Id,
        row.Name,
        row.FormatVersion,
        row.IsBuiltIn,
        new PaletteVariant(row.LightCanvas, row.LightSurface, row.LightAccent),
        new PaletteVariant(row.DarkCanvas, row.DarkSurface, row.DarkAccent));

    private static PaletteRow ToRow(AppearancePalette palette) => new()
    {
        Id = palette.Id,
        Name = palette.Name,
        FormatVersion = palette.FormatVersion,
        IsBuiltIn = palette.IsBuiltIn,
        LightCanvas = palette.Light.Canvas,
        LightSurface = palette.Light.Surface,
        LightAccent = palette.Light.Accent,
        DarkCanvas = palette.Dark.Canvas,
        DarkSurface = palette.Dark.Surface,
        DarkAccent = palette.Dark.Accent,
    };
}
