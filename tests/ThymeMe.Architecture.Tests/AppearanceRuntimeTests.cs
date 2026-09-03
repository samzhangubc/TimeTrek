using ThymeMe.Application.Settings;
using ThymeMe.Presentation.WinUI.Appearance;

namespace ThymeMe.Architecture.Tests;

public sealed class AppearanceRuntimeTests
{
    [Theory]
    [InlineData(AppearanceMode.Light, 23, false)]
    [InlineData(AppearanceMode.Dark, 12, true)]
    [InlineData(AppearanceMode.FollowWindows, 12, true)]
    [InlineData(AppearanceMode.Scheduled, 6, true)]
    [InlineData(AppearanceMode.Scheduled, 12, false)]
    [InlineData(AppearanceMode.Scheduled, 20, true)]
    public void ResolveDarkAppliesModeWithoutRestart(AppearanceMode mode, int hour, bool expected)
    {
        bool actual = AppearanceRuntime.ResolveDark(
            mode,
            new TimeOnly(hour, 0),
            windowsDark: true,
            new TimeOnly(7, 0),
            new TimeOnly(19, 0));

        Assert.Equal(expected, actual);
    }
}
