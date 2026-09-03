using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ThymeMe.Application.Appearance;
using ThymeMe.Application.Settings;
using ThymeMe.Domain.Appearance;
using ThymeMe.Presentation.WinUI.Appearance;
using ThymeMe.Presentation.WinUI.Archive;
using ThymeMe.Presentation.WinUI.History;
using ThymeMe.Presentation.WinUI.Home;
using ThymeMe.Presentation.WinUI.Onboarding;
using ThymeMe.Presentation.WinUI.Reporting;
using ThymeMe.Presentation.WinUI.Settings;
using ThymeMe.Presentation.WinUI.Timing;
using Windows.System;
using Windows.UI.ViewManagement;

namespace ThymeMe.Presentation.WinUI.Shell;

public sealed partial class AppShell : UserControl
{
    private readonly DispatcherTimer displayTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer appearanceTimer = new() { Interval = TimeSpan.FromSeconds(30) };
    private readonly UISettings uiSettings = new();
    private readonly AccessibilitySettings accessibilitySettings = new();
    private readonly TimeProvider timeProvider;
    private readonly TransportControl transport;
    private readonly FirstRunWizardView wizard;
    private double pendingManipulationScale = 1;
    private string? appliedPaletteKey;
    private bool highContrastEventSubscribed;

    public event Action<ElementTheme, PaletteVariant>? AppearanceChanged;

    public ElementTheme CurrentTheme { get; private set; } = ElementTheme.Default;

    public PaletteVariant CurrentPalette { get; private set; } = AppearancePaletteService.BuiltIn[0].Palette.Light;

    public AppShell(AppShellViewModel viewModel, TimeProvider timeProvider)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        InitializeComponent();
        transport = new TransportControl(ViewModel.Transport);
        wizard = new FirstRunWizardView(ViewModel);
        wizard.SetupCompleted += OnSetupCompleted;
        WizardHost.Content = wizard;
        TransportHost.Content = transport;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        ViewModel.Settings.SettingsApplied += OnSettingsApplied;
        ViewModel.Settings.RerunSetupRequested += OnRerunSetupRequested;
        displayTimer.Tick += OnDisplayTick;
        appearanceTimer.Tick += OnAppearanceTick;
        displayTimer.Start();
        appearanceTimer.Start();
        uiSettings.ColorValuesChanged += OnWindowsColorsChanged;
        try
        {
            accessibilitySettings.HighContrastChanged += OnHighContrastChanged;
            highContrastEventSubscribed = true;
        }
        catch (COMException exception) when ((uint)exception.HResult == 0x80070490)
        {
            // Some unpackaged Windows builds do not expose this event; the appearance timer still observes the state.
        }
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

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AppShellViewModel.IsSetupComplete) or nameof(AppShellViewModel.SelectedDestination))
        {
            Refresh();
        }
    }

    private void OnSettingsApplied(AppSettings settings)
    {
        NavigationColumn.Width = new GridLength(settings.NavigationWidth);
        UpdateInterfaceScale(settings.IconScale, ViewportRoot.ActualWidth, ViewportRoot.ActualHeight);
        ApplyAppearance(settings);
    }

    private async void OnRerunSetupRequested()
    {
        try
        {
            await ViewModel.Wizard.LoadAsync();
            wizard.Restart();
            ViewModel.IsSetupComplete = false;
        }
        catch (Exception exception)
        {
            ViewModel.Settings.SaveStatus = $"Setup wizard could not open: {exception.Message}";
        }
    }

    private void OnAppearanceTick(object? sender, object e) => ApplyAppearance(ViewModel.Settings.Current);

    private void OnWindowsColorsChanged(UISettings sender, object args) =>
        _ = DispatcherQueue.TryEnqueue(() => ApplyAppearance(ViewModel.Settings.Current));

    private void OnHighContrastChanged(AccessibilitySettings sender, object args) =>
        _ = DispatcherQueue.TryEnqueue(() => ApplyAppearance(ViewModel.Settings.Current));

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
        appearanceTimer.Stop();
        displayTimer.Tick -= OnDisplayTick;
        appearanceTimer.Tick -= OnAppearanceTick;
        uiSettings.ColorValuesChanged -= OnWindowsColorsChanged;
        if (highContrastEventSubscribed)
        {
            accessibilitySettings.HighContrastChanged -= OnHighContrastChanged;
        }
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.Settings.SettingsApplied -= OnSettingsApplied;
        ViewModel.Settings.RerunSetupRequested -= OnRerunSetupRequested;
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
        ApplyAppearance(settings);

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

        _ = await ViewModel.Settings.SaveAsync(ViewModel.Settings.Current with { IconScale = normalized });
    }

    private static bool IsControlDown() =>
        InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

    public void ApplyCurrentAppearance() => ApplyAppearance(ViewModel.Settings.Current);

    private void ApplyAppearance(AppSettings settings)
    {
        bool windowsDark = uiSettings.GetColorValue(UIColorType.Background) is { R: < 128, G: < 128, B: < 128 };
        bool dark = AppearanceRuntime.ResolveDark(
            settings.AppearanceMode,
            TimeOnly.FromDateTime(timeProvider.GetLocalNow().DateTime),
            windowsDark,
            settings.ScheduledLightStart,
            settings.ScheduledDarkStart);
        AppearancePaletteOption palette = ViewModel.Settings.ResolvePalette(settings.SelectedPaletteId);
        PaletteVariant variant = dark ? palette.Palette.Dark : palette.Palette.Light;

        if (accessibilitySettings.HighContrast)
        {
            RequestedTheme = ElementTheme.Default;
            CurrentTheme = ElementTheme.Default;
            CurrentPalette = variant;
            appliedPaletteKey = null;
            AppearanceChanged?.Invoke(CurrentTheme, CurrentPalette);
            return;
        }

        ElementTheme theme = dark ? ElementTheme.Dark : ElementTheme.Light;
        bool paletteChanged = !string.Equals(appliedPaletteKey, $"{palette.Key}:{dark}", StringComparison.Ordinal);
        AppearanceRuntime.ApplyResources(Resources, variant);
        if (paletteChanged && RequestedTheme == theme)
        {
            RequestedTheme = dark ? ElementTheme.Light : ElementTheme.Dark;
        }

        RequestedTheme = theme;
        CurrentTheme = theme;
        CurrentPalette = variant;
        appliedPaletteKey = $"{palette.Key}:{dark}";
        AppearanceChanged?.Invoke(theme, variant);
    }
}
