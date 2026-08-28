using CommunityToolkit.Mvvm.ComponentModel;
using ThymeMe.Application.DataPortability;
using ThymeMe.Application.Lifecycle;
using ThymeMe.Application.Organization;
using ThymeMe.Application.Settings;

namespace ThymeMe.Presentation.WinUI.Onboarding;

public sealed partial class FirstRunWizardViewModel(
    AppBootstrapService bootstrapService,
    IAppSettingsStore settingsStore,
    OrganizationService organizationService,
    IStartupRegistrationService startupRegistration,
    IDataPortabilityService dataPortability,
    IUserFileDialogService fileDialogs) : ObservableObject
{
    private string? restorePath;
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool CreateMathSample { get; set; }

    [ObservableProperty]
    public partial bool CreateWorkSample { get; set; }

    [ObservableProperty]
    public partial bool StartWithWindows { get; set; }

    [ObservableProperty]
    public partial bool EnableRounding { get; set; }

    [ObservableProperty]
    public partial bool EnableBilling { get; set; }

    [ObservableProperty]
    public partial bool EnableForegroundTracking { get; set; }

    [ObservableProperty]
    public partial int DefaultSessionMinutes { get; set; } = 25;

    [ObservableProperty]
    public partial int PomodoroWorkMinutes { get; set; } = 25;

    [ObservableProperty]
    public partial int PomodoroBreakMinutes { get; set; } = 5;

    [ObservableProperty]
    public partial bool NotificationsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial WeekStartDay WeekStart { get; set; } = WeekStartDay.WindowsDefault;

    [ObservableProperty]
    public partial AppearanceMode AppearanceMode { get; set; } = AppearanceMode.FollowWindows;

    [ObservableProperty]
    public partial bool InterruptionHandlingEnabled { get; set; } = true;

    [ObservableProperty]
    public partial int InactivityThresholdMinutes { get; set; } = 10;

    public async ValueTask LoadAsync(CancellationToken cancellationToken = default)
    {
        AppSettings settings = await settingsStore.LoadAsync(cancellationToken);
        StartWithWindows = settings.StartWithWindows;
        EnableRounding = settings.RoundingEnabled;
        EnableBilling = settings.BillingEnabled;
        EnableForegroundTracking = settings.ForegroundTrackingEnabled;
        DefaultSessionMinutes = settings.DefaultSessionMinutes;
        PomodoroWorkMinutes = settings.PomodoroWorkMinutes;
        PomodoroBreakMinutes = settings.PomodoroBreakMinutes;
        NotificationsEnabled = settings.NotificationsEnabled;
        WeekStart = settings.WeekStart;
        AppearanceMode = settings.AppearanceMode;
        InterruptionHandlingEnabled = settings.InterruptionHandlingEnabled;
        InactivityThresholdMinutes = settings.InactivityThresholdMinutes;
    }

    public async ValueTask SaveProgressAsync(CancellationToken cancellationToken = default)
    {
        AppSettings current = await settingsStore.LoadAsync(cancellationToken);
        await settingsStore.SaveAsync(BuildSettings(current, basicSetupCompleted: false), cancellationToken);
    }

    public async ValueTask<AppSettings> UseRecommendedAsync(CancellationToken cancellationToken = default) =>
        await bootstrapService.UseRecommendedDefaultsAsync(cancellationToken);

    public async ValueTask<AppSettings> CompleteStepByStepAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            AppSettings current = await settingsStore.LoadAsync(cancellationToken);
            AppSettings settings = BuildSettings(current, basicSetupCompleted: true);
            await settingsStore.SaveAsync(settings, cancellationToken);
            await startupRegistration.SetEnabledAsync(settings.StartWithWindows, cancellationToken);
            if (CreateMathSample)
            {
                await CreateMathSampleAsync(cancellationToken);
            }

            if (CreateWorkSample)
            {
                await CreateWorkSampleAsync(cancellationToken);
            }

            return settings;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async ValueTask<RestorePreview?> PreviewRestoreAsync(CancellationToken cancellationToken = default)
    {
        restorePath = await fileDialogs.PickOpenPathAsync("Thyme-Me backup", [".zip"], cancellationToken);
        if (restorePath is null)
        {
            return null;
        }

        await using FileStream stream = OpenBackup(restorePath);
        return await dataPortability.PreviewRestoreAsync(stream, RestoreMode.Replace, cancellationToken);
    }

    public async ValueTask<AppSettings> RestoreAsync(RestoreMode mode, CancellationToken cancellationToken = default)
    {
        string path = restorePath ?? throw new InvalidOperationException("Choose and validate a backup first.");
        await using FileStream stream = OpenBackup(path);
        await dataPortability.RestoreAsync(stream, mode, cancellationToken);
        restorePath = null;
        return await settingsStore.LoadAsync(cancellationToken);
    }

    private static FileStream OpenBackup(string path) => new(path, new FileStreamOptions
    {
        Access = FileAccess.Read,
        Mode = FileMode.Open,
        Share = FileShare.Read,
        Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
    });

    private AppSettings BuildSettings(AppSettings current, bool basicSetupCompleted) => current with
    {
        BasicSetupCompleted = basicSetupCompleted,
        StartWithWindows = StartWithWindows,
        RoundingEnabled = EnableRounding,
        BillingEnabled = EnableBilling,
        ForegroundTrackingEnabled = EnableForegroundTracking,
        DefaultSessionMinutes = DefaultSessionMinutes,
        PomodoroWorkMinutes = PomodoroWorkMinutes,
        PomodoroBreakMinutes = PomodoroBreakMinutes,
        NotificationsEnabled = NotificationsEnabled,
        WeekStart = WeekStart,
        AppearanceMode = AppearanceMode,
        InterruptionHandlingEnabled = InterruptionHandlingEnabled,
        InactivityThresholdMinutes = InactivityThresholdMinutes,
    };

    private async Task CreateMathSampleAsync(CancellationToken cancellationToken)
    {
        Domain.Organization.StreamDefinition? stream = (await organizationService.CreateStreamAsync("MATH 100", cancellationToken: cancellationToken)).Value;
        if (stream is not null)
        {
            _ = await organizationService.CreateCategoryAsync("Lecture", stream.Id, false, null, null, cancellationToken);
            _ = await organizationService.CreateCategoryAsync("Homework", stream.Id, false, null, null, cancellationToken);
        }
    }

    private async Task CreateWorkSampleAsync(CancellationToken cancellationToken)
    {
        Domain.Organization.StreamDefinition? stream = (await organizationService.CreateStreamAsync("Work", cancellationToken: cancellationToken)).Value;
        if (stream is not null)
        {
            _ = await organizationService.CreateCategoryAsync("Focused work", stream.Id, false, null, null, cancellationToken);
            _ = await organizationService.CreateProjectAsync("Example project", stream.Id, cancellationToken);
        }
    }
}
