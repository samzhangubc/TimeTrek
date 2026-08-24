using System.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using TimeTrek.Application.Settings;
using TimeTrek.Presentation.WinUI.Archive;
using TimeTrek.Presentation.WinUI.History;
using TimeTrek.Presentation.WinUI.Home;
using TimeTrek.Presentation.WinUI.Onboarding;
using TimeTrek.Presentation.WinUI.Reporting;
using TimeTrek.Presentation.WinUI.Settings;
using TimeTrek.Presentation.WinUI.Timing;
using Windows.System;

namespace TimeTrek.Presentation.WinUI.Shell;

public sealed partial class AppShell : UserControl
{
    private readonly DispatcherTimer displayTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly TransportControl transport;
    private readonly FirstRunWizardView wizard;
    private double pendingManipulationScale = 1;

    public AppShell(AppShellViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        transport = new TransportControl(ViewModel.Transport);
        wizard = new FirstRunWizardView(ViewModel);
        wizard.SetupCompleted += OnSetupCompleted;
        WizardHost.Content = wizard;
        TransportHost.Content = transport;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        displayTimer.Tick += OnDisplayTick;
        displayTimer.Start();
        Unloaded += OnUnloaded;
        Refresh();
    }

    public AppShellViewModel ViewModel { get; }

    private void OnSetupCompleted(object? sender, EventArgs e) => Refresh();

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

    private async void OnDisplayTick(object? sender, object e)
    {
        await transport.RefreshAsync();
        if (ViewModel.SelectedDestination == ShellDestination.Home)
        {
            await ViewModel.Home.RefreshTimingAsync();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        displayTimer.Stop();
        displayTimer.Tick -= OnDisplayTick;
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        wizard.SetupCompleted -= OnSetupCompleted;
        Unloaded -= OnUnloaded;
    }

    private void Refresh()
    {
        WizardHost.Visibility = ViewModel.IsSetupComplete ? Visibility.Collapsed : Visibility.Visible;
        MainRoot.Visibility = ViewModel.IsSetupComplete ? Visibility.Visible : Visibility.Collapsed;
        TransportHost.Visibility = ViewModel.SelectedDestination == ShellDestination.Settings
            ? Visibility.Collapsed
            : Visibility.Visible;

        AppSettings settings = ViewModel.Settings.Current;
        FontSize = 14;
        NavigationColumn.Width = new GridLength(settings.NavigationWidth);
        UpdateInterfaceScale(settings.IconScale, ViewportRoot.ActualWidth, ViewportRoot.ActualHeight);
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

    private async void OnScaleKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (!IsControlDown())
        {
            return;
        }

        double? scale = e.Key switch
        {
            VirtualKey.Add or (VirtualKey)187 => ViewModel.Settings.Current.IconScale + 0.1,
            VirtualKey.Subtract or (VirtualKey)189 => ViewModel.Settings.Current.IconScale - 0.1,
            VirtualKey.Number0 or VirtualKey.NumberPad0 => 1,
            _ => null,
        };
        if (scale is double value)
        {
            e.Handled = true;
            await ApplyInterfaceScaleAsync(value);
        }
    }

    private async void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (!IsControlDown())
        {
            return;
        }

        e.Handled = true;
        int delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
        await ApplyInterfaceScaleAsync(ViewModel.Settings.Current.IconScale + (delta > 0 ? 0.1 : -0.1));
    }

    private async void OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        pendingManipulationScale *= e.Delta.Scale;
        if (pendingManipulationScale is > 1.08 or < 0.92)
        {
            double change = pendingManipulationScale > 1 ? 0.1 : -0.1;
            pendingManipulationScale = 1;
            e.Handled = true;
            await ApplyInterfaceScaleAsync(ViewModel.Settings.Current.IconScale + change);
        }
    }

    private void OnViewportSizeChanged(object sender, SizeChangedEventArgs e) =>
        UpdateInterfaceScale(ViewModel.Settings.Current.IconScale, e.NewSize.Width, e.NewSize.Height);

    private void UpdateInterfaceScale(double scale, double viewportWidth, double viewportHeight)
    {
        if (viewportWidth <= 0 || viewportHeight <= 0)
        {
            return;
        }

        InterfaceScaleTransform.ScaleX = scale;
        InterfaceScaleTransform.ScaleY = scale;
        ScaleRoot.Width = viewportWidth / scale;
        ScaleRoot.Height = viewportHeight / scale;
    }

    private async ValueTask ApplyInterfaceScaleAsync(double value)
    {
        double normalized = Math.Round(Math.Clamp(value, 0.8, 1.5), 1);
        if (Math.Abs(normalized - ViewModel.Settings.Current.IconScale) < 0.001)
        {
            return;
        }

        await ViewModel.Settings.SaveAsync(ViewModel.Settings.Current with { IconScale = normalized });
        Refresh();
    }

    private static bool IsControlDown() =>
        InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

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
