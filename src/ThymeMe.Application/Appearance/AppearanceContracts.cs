using System.Text.Json;
using ThymeMe.Domain.Appearance;

namespace ThymeMe.Application.Appearance;

public sealed record AppearancePaletteOption(string Key, AppearancePalette Palette);

public interface IAppearancePaletteStore
{
    ValueTask<IReadOnlyList<AppearancePalette>> ListCustomAsync(CancellationToken cancellationToken = default);

    ValueTask AddAsync(AppearancePalette palette, CancellationToken cancellationToken = default);

    ValueTask UpdateAsync(AppearancePalette palette, CancellationToken cancellationToken = default);

    ValueTask DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class AppearancePaletteService(IAppearancePaletteStore store)
{
    public const int MaximumCustomPalettes = 64;
    public const int MaximumPaletteFileBytes = 64 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        MaxDepth = 16,
    };

    public static IReadOnlyList<AppearancePaletteOption> BuiltIn { get; } =
    [
        BuiltInPalette("github-default", "GitHub Default", "019d0000-0000-7000-8000-000000000001", "#FFFFFF", "#F6F8FA", "#0969DA", "#0D1117", "#161B22", "#58A6FF"),
        BuiltInPalette("codex-plus", "Codex Plus", "019d0000-0000-7000-8000-000000000002", "#FFFFFF", "#F4F4F4", "#0E7C66", "#181818", "#242424", "#10A37F"),
        BuiltInPalette("catppuccin", "Catppuccin", "019d0000-0000-7000-8000-000000000003", "#EFF1F5", "#E6E9EF", "#1E66F5", "#1E1E2E", "#313244", "#89B4FA"),
        BuiltInPalette("gruvbox-medium", "Gruvbox Medium", "019d0000-0000-7000-8000-000000000004", "#FBF1C7", "#EBDBB2", "#458588", "#282828", "#3C3836", "#83A598"),
        BuiltInPalette("solarized", "Solarized", "019d0000-0000-7000-8000-000000000005", "#FDF6E3", "#EEE8D5", "#268BD2", "#002B36", "#073642", "#2AA198"),
        BuiltInPalette("claude-warm", "Claude-inspired Warm", "019d0000-0000-7000-8000-000000000006", "#FFF8F0", "#F3E8DC", "#C15F3C", "#1F1A17", "#2B2420", "#E38B6D"),
        BuiltInPalette("google-material", "Google-inspired Material", "019d0000-0000-7000-8000-000000000007", "#FFFFFF", "#F5F5F5", "#1A73E8", "#202124", "#303134", "#8AB4F8"),
        BuiltInPalette("nord", "Nord-inspired", "019d0000-0000-7000-8000-000000000008", "#ECEFF4", "#E5E9F0", "#5E81AC", "#2E3440", "#3B4252", "#88C0D0"),
        BuiltInPalette("dracula", "Dracula-inspired", "019d0000-0000-7000-8000-000000000009", "#F8F8F2", "#EDECF3", "#6D3FC0", "#282A36", "#44475A", "#BD93F9"),
        BuiltInPalette("monokai", "Monokai-inspired", "019d0000-0000-7000-8000-000000000010", "#FAFAFA", "#F0F0F0", "#B0004B", "#272822", "#3E3D32", "#F92672"),
    ];

    public async ValueTask<IReadOnlyList<AppearancePaletteOption>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AppearancePalette> custom = await store.ListCustomAsync(cancellationToken).ConfigureAwait(false);
        return [.. BuiltIn, .. custom.Select(item => new AppearancePaletteOption(CustomKey(item.Id), item))];
    }

    public async ValueTask<AppearancePaletteOption> CreateAsync(
        string name,
        PaletteVariant light,
        PaletteVariant dark,
        CancellationToken cancellationToken = default)
    {
        await EnsureCapacityAsync(cancellationToken).ConfigureAwait(false);
        AppearancePalette palette = new(
            Guid.CreateVersion7(), name, AppearancePalette.CurrentFormatVersion, false, light, dark);
        palette = palette.Validate();
        await store.AddAsync(palette, cancellationToken).ConfigureAwait(false);
        return new AppearancePaletteOption(CustomKey(palette.Id), palette);
    }

    public async ValueTask<AppearancePaletteOption> UpdateAsync(
        Guid id,
        string name,
        PaletteVariant light,
        PaletteVariant dark,
        CancellationToken cancellationToken = default)
    {
        AppearancePalette palette = new(
            id, name, AppearancePalette.CurrentFormatVersion, false, light, dark);
        palette = palette.Validate();
        await store.UpdateAsync(palette, cancellationToken).ConfigureAwait(false);
        return new AppearancePaletteOption(CustomKey(palette.Id), palette);
    }

    public async ValueTask<AppearancePaletteOption> DuplicateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureCapacityAsync(cancellationToken).ConfigureAwait(false);
        AppearancePalette source = (await store.ListCustomAsync(cancellationToken).ConfigureAwait(false))
            .SingleOrDefault(item => item.Id == id)
            ?? throw new InvalidOperationException("The custom palette no longer exists.");
        return await CreateAsync($"{source.Name} copy", source.Light, source.Dark, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        store.DeleteAsync(id, cancellationToken);

    public async ValueTask ExportAsync(Guid id, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        AppearancePalette palette = (await store.ListCustomAsync(cancellationToken).ConfigureAwait(false))
            .SingleOrDefault(item => item.Id == id)
            ?? throw new InvalidOperationException("The custom palette no longer exists.");
        await JsonSerializer.SerializeAsync(destination, palette, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<AppearancePaletteOption> ImportAsync(Stream source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.CanSeek && source.Length is <= 0 or > MaximumPaletteFileBytes)
        {
            throw new InvalidDataException("The palette file has an invalid size.");
        }

        await EnsureCapacityAsync(cancellationToken).ConfigureAwait(false);
        await using MemoryStream boundedSource = await ReadBoundedAsync(source, cancellationToken).ConfigureAwait(false);
        AppearancePalette palette = await JsonSerializer.DeserializeAsync<AppearancePalette>(boundedSource, JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("The palette file is empty.");
        palette = (palette with { IsBuiltIn = false }).Validate();
        if (BuiltIn.Any(item => item.Palette.Id == palette.Id) ||
            (await store.ListCustomAsync(cancellationToken).ConfigureAwait(false)).Any(item => item.Id == palette.Id))
        {
            throw new InvalidDataException("A palette with this identifier already exists.");
        }

        await store.AddAsync(palette, cancellationToken).ConfigureAwait(false);
        return new AppearancePaletteOption(CustomKey(palette.Id), palette);
    }

    public static string CustomKey(Guid id) => $"custom:{id:D}";

    public static bool TryParseCustomKey(string? key, out Guid id) =>
        Guid.TryParse(key?.StartsWith("custom:", StringComparison.Ordinal) == true ? key[7..] : null, out id);

    public static AppearancePaletteOption Resolve(IReadOnlyList<AppearancePaletteOption> palettes, string? key) =>
        palettes.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.Ordinal))
        ?? BuiltIn[0];

    private async ValueTask EnsureCapacityAsync(CancellationToken cancellationToken)
    {
        if ((await store.ListCustomAsync(cancellationToken).ConfigureAwait(false)).Count >= MaximumCustomPalettes)
        {
            throw new InvalidOperationException($"At most {MaximumCustomPalettes} custom palettes are supported.");
        }
    }

    private static async ValueTask<MemoryStream> ReadBoundedAsync(Stream source, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[MaximumPaletteFileBytes + 1];
        int total = 0;
        while (total < buffer.Length)
        {
            int read = await source.ReadAsync(buffer.AsMemory(total, buffer.Length - total), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            total += read;
        }

        if (total == 0 || total > MaximumPaletteFileBytes)
        {
            throw new InvalidDataException("The palette file has an invalid size.");
        }

        return new MemoryStream(buffer, 0, total, writable: false, publiclyVisible: false);
    }

    private static AppearancePaletteOption BuiltInPalette(
        string key,
        string name,
        string id,
        string lightCanvas,
        string lightSurface,
        string lightAccent,
        string darkCanvas,
        string darkSurface,
        string darkAccent) =>
        new(
            key,
            new AppearancePalette(
                Guid.Parse(id),
                name,
                AppearancePalette.CurrentFormatVersion,
                true,
                new PaletteVariant(lightCanvas, lightSurface, lightAccent),
                new PaletteVariant(darkCanvas, darkSurface, darkAccent)).Validate());
}
