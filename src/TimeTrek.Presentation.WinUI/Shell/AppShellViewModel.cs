using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeTrek.Application.DataPortability;
using TimeTrek.Application.Settings;
using TimeTrek.Presentation.WinUI.Archive;
using TimeTrek.Presentation.WinUI.History;
using TimeTrek.Presentation.WinUI.Home;
using TimeTrek.Presentation.WinUI.Onboarding;
using TimeTrek.Presentation.WinUI.Reporting;
using TimeTrek.Presentation.WinUI.Settings;
using TimeTrek.Presentation.WinUI.Timing;

namespace TimeTrek.Presentation.WinUI.Shell;

public sealed partial class AppShellViewModel(
    AppBootstrapService bootstrapService,
    HomeViewModel home,
    HistoryViewModel history,
    StatsViewModel stats,
    ArchiveViewModel archive,
    SettingsViewModel settings,
    TransportViewModel transport,
    FirstRunWizardViewModel wizard) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageTitle))]
    public partial ShellDestination SelectedDestination { get; set; }

    [ObservableProperty]
    public partial bool IsSetupComplete { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool ShowStepByStep { get; set; }

    public string PageTitle => SelectedDestination.ToString();

    public HomeViewModel Home { get; } = home;

    public HistoryViewModel History { get; } = history;

    public StatsViewModel Stats { get; } = stats;

    public ArchiveViewModel Archive { get; } = archive;

    public SettingsViewModel Settings { get; } = settings;

    public TransportViewModel Transport { get; } = transport;

    public FirstRunWizardViewModel Wizard { get; } = wizard;

    public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        AppSettings appSettings = await bootstrapService.LoadAsync(cancellationToken);
        await Settings.LoadAsync(cancellationToken);
        IsSetupComplete = appSettings.BasicSetupCompleted;
        if (IsSetupComplete)
        {
            await LoadDestinationAsync(SelectedDestination, cancellationToken);
            await Transport.LoadAsync(cancellationToken);
        }
    }

    [RelayCommand]
    private async Task NavigateAsync(ShellDestination destination)
    {
        SelectedDestination = destination;
        await LoadDestinationAsync(destination);
    }

    [RelayCommand]
    private void BeginStepByStep() => ShowStepByStep = true;

    [RelayCommand]
    private async Task UseRecommendedDefaultsAsync() =>
        await CompleteSetupAsync(() => Wizard.UseRecommendedAsync());

    [RelayCommand]
    private async Task CompleteStepByStepAsync() =>
        await CompleteSetupAsync(() => Wizard.CompleteStepByStepAsync());

    public async ValueTask<RestorePreview?> PreviewRestoreAsync(CancellationToken cancellationToken = default) =>
        await Wizard.PreviewRestoreAsync(cancellationToken);

    public async ValueTask RestoreAsync(RestoreMode mode, CancellationToken cancellationToken = default) =>
        await CompleteSetupAsync(() => Wizard.RestoreAsync(mode, cancellationToken));

    private async Task CompleteSetupAsync(Func<ValueTask<AppSettings>> operation)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            AppSettings appSettings = await operation();
            IsSetupComplete = appSettings.BasicSetupCompleted;
            SelectedDestination = ShellDestination.Home;
            await Home.LoadAsync();
            await Transport.LoadAsync();
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private ValueTask LoadDestinationAsync(
        ShellDestination destination,
        CancellationToken cancellationToken = default) => destination switch
        {
            ShellDestination.Home => Home.LoadAsync(cancellationToken),
            ShellDestination.History => History.LoadAsync(cancellationToken),
            ShellDestination.Stats => Stats.LoadAsync(cancellationToken),
            ShellDestination.Archive => Archive.LoadAsync(cancellationToken),
            ShellDestination.Settings => Settings.LoadAsync(cancellationToken),
            _ => ValueTask.CompletedTask,
        };
}
