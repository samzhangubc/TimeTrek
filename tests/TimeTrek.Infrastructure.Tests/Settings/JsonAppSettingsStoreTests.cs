using TimeTrek.Application.Settings;
using TimeTrek.Infrastructure.Settings;

namespace TimeTrek.Infrastructure.Tests.Settings;

public sealed class JsonAppSettingsStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "TimeTrek.Tests",
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
        };

        await store.SaveAsync(expected);
        AppSettings actual = await store.LoadAsync();

        Assert.Equal(expected, actual);
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
