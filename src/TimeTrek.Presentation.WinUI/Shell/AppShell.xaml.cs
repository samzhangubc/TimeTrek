using System.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TimeTrek.Application.DataPortability;
using TimeTrek.Application.Settings;
using TimeTrek.Presentation.WinUI.Archive;
using TimeTrek.Presentation.WinUI.History;
using TimeTrek.Presentation.WinUI.Home;
using TimeTrek.Presentation.WinUI.Reporting;
using TimeTrek.Presentation.WinUI.Settings;
using TimeTrek.Presentation.WinUI.Timing;

namespace TimeTrek.Presentation.WinUI.Shell;

public sealed partial class AppShell : UserControl
{
    private readonly DispatcherTimer displayTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly TransportControl transport;

    public AppShell(AppShellViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        transport = new TransportControl(ViewModel.Transport);
        TransportHost.Content = transport;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        displayTimer.Tick += OnDisplayTick;
        displayTimer.Start();
        Unloaded += OnUnloaded;
        Refresh();
    }

    public AppShellViewModel ViewModel { get; }

    private async void OnUseRecommendedDefaults(object sender, RoutedEventArgs e)
    {
        await ViewModel.UseRecommendedDefaultsCommand.ExecuteAsync(null);
        Refresh();
    }

    private void OnBeginStepByStep(object sender, RoutedEventArgs e)
    {
        ViewModel.BeginStepByStepCommand.Execute(null);
        Refresh();
    }

    private async void OnCompleteStepByStep(object sender, RoutedEventArgs e)
    {
        await ViewModel.CompleteStepByStepCommand.ExecuteAsync(null);
        Refresh();
    }

    private async void OnRestoreBackup(object sender, RoutedEventArgs e)
    {
        try
        {
            RestorePreview? preview = await ViewModel.PreviewRestoreAsync();
            if (preview is null)
            {
                return;
            }

            ContentDialog dialog = new()
            {
                XamlRoot = XamlRoot,
                Title = "Restore validated backup?",
                Content = $"Replace current setup with {preview.StreamCount} Streams, {preview.CategoryCount} Categories, {preview.ProjectCount} Projects, and {preview.SessionCount} Sessions.",
                PrimaryButtonText = "Replace",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await ViewModel.RestoreAsync(RestoreMode.Replace);
                Refresh();
            }
        }
        catch (Exception exception)
        {
            ViewModel.ErrorMessage = exception.Message;
            Refresh();
        }
    }

    private async void OnNavigate(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string destination } &&
            Enum.TryParse(destination, ignoreCase: false, out ShellDestination parsed))
        {
            await ViewModel.NavigateCommand.ExecuteAsync(parsed);
            Refresh();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e) => Refresh();

    private async void OnDisplayTick(object? sender, object e) => await transport.RefreshAsync();

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        displayTimer.Stop();
        displayTimer.Tick -= OnDisplayTick;
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        Unloaded -= OnUnloaded;
    }

    private void Refresh()
    {
        WizardRoot.Visibility = ViewModel.IsSetupComplete ? Visibility.Collapsed : Visibility.Visible;
        MainRoot.Visibility = ViewModel.IsSetupComplete ? Visibility.Visible : Visibility.Collapsed;
        RecommendedDefaultsButton.IsEnabled = !ViewModel.IsBusy;
        WizardError.IsOpen = !string.IsNullOrWhiteSpace(ViewModel.ErrorMessage);
        WizardError.Title = ViewModel.ErrorMessage ?? string.Empty;
        WelcomeActions.Visibility = ViewModel.ShowStepByStep ? Visibility.Collapsed : Visibility.Visible;
        StepByStepPanel.Visibility = ViewModel.ShowStepByStep ? Visibility.Visible : Visibility.Collapsed;
        TransportHost.Visibility = ViewModel.SelectedDestination == ShellDestination.Settings
            ? Visibility.Collapsed
            : Visibility.Visible;

        AppSettings settings = ViewModel.Settings.Current;
        NavigationColumn.Width = new GridLength(settings.NavigationWidth);
        RequestedTheme = ResolveTheme(settings);

        PageHost.Content = ViewModel.SelectedDestination switch
        {
            ShellDestination.Home => new HomePage(ViewModel.Home, ViewModel.Transport),
            ShellDestination.History => new HistoryPage(ViewModel.History, ViewModel.Transport),
            ShellDestination.Stats => new StatsPage(ViewModel.Stats),
            ShellDestination.Archive => new ArchivePage(ViewModel.Archive),
            ShellDestination.Settings => new SettingsPage(ViewModel.Settings),
            _ => throw new InvalidOperationException("Unknown shell destination."),
        };
    }

    private static ElementTheme ResolveTheme(AppSettings settings)
    {
        if (settings.AppearanceMode == AppearanceMode.Scheduled)
        {
            TimeOnly now = TimeOnly.FromDateTime(DateTime.Now);
            bool dark = settings.ScheduledDarkStart > settings.ScheduledLightStart
                ? now >= settings.ScheduledDarkStart || now < settings.ScheduledLightStart
                : now >= settings.ScheduledDarkStart && now < settings.ScheduledLightStart;
            return dark ? ElementTheme.Dark : ElementTheme.Light;
        }

        return settings.AppearanceMode switch
        {
            AppearanceMode.Light => ElementTheme.Light,
            AppearanceMode.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };
    }
}
