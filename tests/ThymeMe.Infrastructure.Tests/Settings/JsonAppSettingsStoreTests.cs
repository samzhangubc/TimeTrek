using ThymeMe.Application.Settings;
using ThymeMe.Infrastructure.Settings;

namespace ThymeMe.Infrastructure.Tests.Settings;

public sealed class JsonAppSettingsStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "ThymeMe.Tests",
        Guid.CreateVersion7().ToString("N"));

    [Fact]
    public async Task LoadAsyncReturnsIncompleteDefaultsWhenNoFileExists()
    {
        using JsonAppSettingsStore store = new(directory);

        AppSettings settings = await store.LoadAsync();

        Assert.False(settings.BasicSetupCompleted);
        Assert.Equal(25, settings.DefaultSessionMinutes);
    }

    [Fact]
    public async Task SaveAsyncRoundTripsValidatedSettings()
    {
        using JsonAppSettingsStore store = new(directory);
        AppSettings expected = AppSettings.RecommendedDefaults with
        {
            NavigationWidth = 72,
            SharedChannelWidth = 200,
            IconScale = 1.25,
            WindowX = 120,
            WindowY = 80,
            WindowWidth = 1440,
            WindowHeight = 900,
            WindowMaximized = true,
            PaletteContrastAcknowledgements = new Dictionary<string, string>
            {
                ["custom:01992ef0-36c4-7582-8e50-b55c2dbf8120"] = "palette-signature",
            },
            StreamCategoryDefaults = new Dictionary<Guid, Guid[]>
            {
                [Guid.Parse("01992ef0-36c4-7582-8e50-b55c2dbf8121")] =
                [Guid.Parse("01992ef0-36c4-7582-8e50-b55c2dbf8122")],
            },
            StreamProjectDefaults = new Dictionary<Guid, Guid?>
            {
                [Guid.Parse("01992ef0-36c4-7582-8e50-b55c2dbf8121")] =
                    Guid.Parse("01992ef0-36c4-7582-8e50-b55c2dbf8123"),
            },
        };

        await store.SaveAsync(expected);
        AppSettings actual = await store.LoadAsync();

        Assert.Equal(expected with
        {
            StreamCategoryDefaults = actual.StreamCategoryDefaults,
            StreamProjectDefaults = actual.StreamProjectDefaults,
            PaletteContrastAcknowledgements = actual.PaletteContrastAcknowledgements,
        }, actual);
        Assert.Equal(expected.PaletteContrastAcknowledgements, actual.PaletteContrastAcknowledgements);
        KeyValuePair<Guid, Guid[]> categoryDefault = Assert.Single(actual.StreamCategoryDefaults);
        Assert.Equal(Assert.Single(expected.StreamCategoryDefaults).Value, categoryDefault.Value);
        Assert.Equal(expected.StreamCategoryDefaults.Keys, actual.StreamCategoryDefaults.Keys);
        Assert.Equal(Assert.Single(expected.StreamProjectDefaults), Assert.Single(actual.StreamProjectDefaults));
    }

    [Fact]
    public async Task LoadAsyncDoesNotReplaceMalformedJsonWithDefaults()
    {
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "settings.json");
        await File.WriteAllTextAsync(path, "{not valid json");
        using JsonAppSettingsStore store = new(directory);

        await Assert.ThrowsAsync<SettingsStoreException>(
            async () => await store.LoadAsync());

        Assert.Equal("{not valid json", await File.ReadAllTextAsync(path));
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
