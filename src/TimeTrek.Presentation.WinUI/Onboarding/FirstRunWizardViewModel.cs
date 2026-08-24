using CommunityToolkit.Mvvm.ComponentModel;
using TimeTrek.Application.DataPortability;
using TimeTrek.Application.Lifecycle;
using TimeTrek.Application.Organization;
using TimeTrek.Application.Settings;

namespace TimeTrek.Presentation.WinUI.Onboarding;

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

    public async ValueTask<AppSettings> UseRecommendedAsync(CancellationToken cancellationToken = default) =>
        await bootstrapService.UseRecommendedDefaultsAsync(cancellationToken);

    public async ValueTask<AppSettings> CompleteStepByStepAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            AppSettings current = await settingsStore.LoadAsync(cancellationToken);
            AppSettings settings = current with
            {
                BasicSetupCompleted = true,
                StartWithWindows = StartWithWindows,
                RoundingEnabled = EnableRounding,
                BillingEnabled = EnableBilling,
                ForegroundTrackingEnabled = EnableForegroundTracking,
            };
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
        restorePath = await fileDialogs.PickOpenPathAsync("TimeTrek backup", [".zip"], cancellationToken);
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
