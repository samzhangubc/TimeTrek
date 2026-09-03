using ThymeMe.Application.Appearance;
using ThymeMe.Domain.Appearance;

namespace ThymeMe.Application.Tests.Appearance;

public sealed class AppearancePaletteServiceTests
{
    [Fact]
    public void BuiltInsUseDocumentedOrderAndValidVariants()
    {
        Assert.Equal(
            ["GitHub Default", "Codex Plus", "Catppuccin", "Gruvbox Medium", "Solarized", "Claude-inspired Warm", "Google-inspired Material", "Nord-inspired", "Dracula-inspired", "Monokai-inspired"],
            AppearancePaletteService.BuiltIn.Select(item => item.Palette.Name));
        Assert.All(AppearancePaletteService.BuiltIn, item => item.Palette.Validate());
    }

    [Fact]
    public async Task ImportRejectsDuplicateIdentifierWithoutMutation()
    {
        MemoryPaletteStore store = new();
        AppearancePaletteService service = new(store);
        AppearancePalette palette = new(
            Guid.CreateVersion7(),
            "Personal",
            AppearancePalette.CurrentFormatVersion,
            false,
            new PaletteVariant("#FFFFFF", "#F0F0F0", "#0055AA"),
            new PaletteVariant("#101010", "#202020", "#66AAFF"));
        await store.AddAsync(palette);
        await using MemoryStream stream = new(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(palette));

        await Assert.ThrowsAsync<InvalidDataException>(async () => await service.ImportAsync(stream));

        Assert.Single(await store.ListCustomAsync());
    }

    [Fact]
    public async Task CreateNormalizesColorsAndUsesCustomKey()
    {
        AppearancePaletteService service = new(new MemoryPaletteStore());

        AppearancePaletteOption created = await service.CreateAsync(
            "Personal",
            new PaletteVariant("#ffffff", "#f0f0f0", "#0055aa"),
            new PaletteVariant("#101010", "#202020", "#66aaff"));

        Assert.StartsWith("custom:", created.Key, StringComparison.Ordinal);
        Assert.Equal("#FFFFFF", created.Palette.Light.Canvas);
        Assert.Equal("#66AAFF", created.Palette.Dark.Accent);
    }

    [Fact]
    public async Task ImportRejectsOversizedNonSeekableInputWithoutMutation()
    {
        MemoryPaletteStore store = new();
        AppearancePaletteService service = new(store);
        await using Stream source = new NonSeekableStream(new byte[AppearancePaletteService.MaximumPaletteFileBytes + 1]);

        await Assert.ThrowsAsync<InvalidDataException>(async () => await service.ImportAsync(source));

        Assert.Empty(await store.ListCustomAsync());
    }

    private sealed class NonSeekableStream(byte[] content) : MemoryStream(content)
    {
        public override bool CanSeek => false;

        public override long Length => throw new NotSupportedException();
    }

    private sealed class MemoryPaletteStore : IAppearancePaletteStore
    {
        private readonly List<AppearancePalette> palettes = [];

        public ValueTask<IReadOnlyList<AppearancePalette>> ListCustomAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<AppearancePalette>>(palettes.ToList());

        public ValueTask AddAsync(AppearancePalette palette, CancellationToken cancellationToken = default)
        {
            palettes.Add(palette);
            return ValueTask.CompletedTask;
        }

        public ValueTask UpdateAsync(AppearancePalette palette, CancellationToken cancellationToken = default)
        {
            int index = palettes.FindIndex(item => item.Id == palette.Id);
            if (index < 0)
            {
                throw new InvalidOperationException();
            }

            palettes[index] = palette;
            return ValueTask.CompletedTask;
        }

        public ValueTask DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            palettes.RemoveAll(item => item.Id == id);
            return ValueTask.CompletedTask;
        }
    }
}
