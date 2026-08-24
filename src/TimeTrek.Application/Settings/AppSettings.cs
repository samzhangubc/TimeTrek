using TimeTrek.Domain.Reporting;
using TimeTrek.Domain.Timing;

namespace TimeTrek.Application.Settings;

public sealed record AppSettings
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public bool BasicSetupCompleted { get; init; }

    public int DefaultSessionMinutes { get; init; } = 25;

    public int PomodoroWorkMinutes { get; init; } = 25;

    public int PomodoroBreakMinutes { get; init; } = 5;

    public int PomodoroBufferMinutes { get; init; } = 5;

    public bool PomodoroBreaksBillable { get; init; }

    public TimingMode LastTimingMode { get; init; } = TimingMode.Normal;

    public bool NotificationsEnabled { get; init; } = true;

    public bool SoundEnabled { get; init; }

    public bool StartWithWindows { get; init; }

    public bool RoundingEnabled { get; init; }

    public int RoundingIncrementMinutes { get; init; } = 5;

    public RoundingRule RoundingRule { get; init; } = RoundingRule.Nearest;

    public bool BillingEnabled { get; init; }

    public bool InterruptionHandlingEnabled { get; init; } = true;

    public int InactivityThresholdMinutes { get; init; } = 10;

    public bool StopOnGenericInactivity { get; init; } = true;

    public bool StopOnDisplayOff { get; init; } = true;

    public bool StopOnScreenSaver { get; init; } = true;

    public bool StopOnLock { get; init; } = true;

    public bool StopOnSuspend { get; init; } = true;

    public bool ForegroundTrackingEnabled { get; init; }

    public AppearanceMode AppearanceMode { get; init; } = AppearanceMode.FollowWindows;

    public TimeOnly ScheduledLightStart { get; init; } = new(7, 0);

    public TimeOnly ScheduledDarkStart { get; init; } = new(19, 0);

    public string SelectedPaletteId { get; init; } = "github-default";

    public WeekStartDay WeekStart { get; init; } = WeekStartDay.WindowsDefault;

    public double NavigationWidth { get; init; } = 64;

    public double SharedChannelWidth { get; init; } = 184;

    public double IconScale { get; init; } = 1;

    public Dictionary<Guid, Guid[]> StreamCategoryDefaults { get; init; } = [];

    public Dictionary<Guid, Guid?> StreamProjectDefaults { get; init; } = [];

    public int? WindowX { get; init; }

    public int? WindowY { get; init; }

    public int WindowWidth { get; init; } = 1280;

    public int WindowHeight { get; init; } = 800;

    public bool WindowMaximized { get; init; }

    public int RecentlyDeletedRetentionDays { get; init; } = 30;

    public bool CheckForUpdatesAutomatically { get; init; } = true;

    public bool DownloadUpdatesAutomatically { get; init; }

    public long? LastUpdateCheckUtcMilliseconds { get; init; }

    public static AppSettings RecommendedDefaults => new()
    {
        BasicSetupCompleted = true,
    };
}

public enum AppearanceMode
{
    Light,
    Dark,
    FollowWindows,
    Scheduled,
}

public enum WeekStartDay
{
    WindowsDefault,
    Sunday,
    Monday,
}
