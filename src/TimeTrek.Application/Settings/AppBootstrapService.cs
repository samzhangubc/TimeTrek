namespace TimeTrek.Application.Settings;

public sealed class AppBootstrapService(IAppSettingsStore settingsStore)
{
    public ValueTask<AppSettings> LoadAsync(CancellationToken cancellationToken = default) =>
        settingsStore.LoadAsync(cancellationToken);

    public async ValueTask<AppSettings> UseRecommendedDefaultsAsync(
        CancellationToken cancellationToken = default)
    {
        AppSettings settings = AppSettings.RecommendedDefaults;
        await settingsStore.SaveAsync(settings, cancellationToken).ConfigureAwait(false);
        return settings;
    }
}
