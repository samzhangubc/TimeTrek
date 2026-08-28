using ThymeMe.Application.Settings;

namespace ThymeMe.Application.Tests.Settings;

public sealed class AppSettingsTests
{
    [Fact]
    public void RecommendedDefaultsAreSafeAndCompleteBasicSetup()
    {
        AppSettings settings = AppSettings.RecommendedDefaults;

        Assert.True(settings.BasicSetupCompleted);
        Assert.Equal(25, settings.DefaultSessionMinutes);
        Assert.Equal(25, settings.PomodoroWorkMinutes);
        Assert.Equal(5, settings.PomodoroBreakMinutes);
        Assert.True(settings.NotificationsEnabled);
        Assert.False(settings.SoundEnabled);
        Assert.False(settings.StartWithWindows);
        Assert.False(settings.RoundingEnabled);
        Assert.False(settings.BillingEnabled);
        Assert.True(settings.InterruptionHandlingEnabled);
        Assert.False(settings.ForegroundTrackingEnabled);
        Assert.Equal(AppearanceMode.FollowWindows, settings.AppearanceMode);
    }
}
