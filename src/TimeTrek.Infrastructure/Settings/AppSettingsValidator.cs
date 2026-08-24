using TimeTrek.Application.Settings;

namespace TimeTrek.Infrastructure.Settings;

internal static class AppSettingsValidator
{
    public static AppSettings Validate(AppSettings settings)
    {
        if (settings.DefaultSessionMinutes is < 1 or > 10080 ||
            settings.PomodoroWorkMinutes is < 1 or > 1440 ||
            settings.PomodoroBreakMinutes is < 1 or > 1440 ||
            settings.PomodoroBufferMinutes is < 1 or > 60 ||
            settings.InactivityThresholdMinutes is < 1 or > 1440 ||
            settings.RoundingIncrementMinutes is < 1 or > 60 ||
            settings.RecentlyDeletedRetentionDays is < 1 or > 365 ||
            settings.NavigationWidth is < 48 or > 160 ||
            settings.SharedChannelWidth is < 144 or > 360 ||
            settings.IconScale is < 0.75 or > 2 ||
            settings.SelectedPaletteId.Length is 0 or > 200)
        {
            throw new SettingsStoreException("The settings file contains an out-of-range value.");
        }

        if (!settings.CheckForUpdatesAutomatically && settings.DownloadUpdatesAutomatically)
        {
            throw new SettingsStoreException("Automatic download requires automatic update checks.");
        }

        return settings;
    }
}
