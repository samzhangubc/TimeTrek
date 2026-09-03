using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using ThymeMe.Application.Appearance;
using ThymeMe.Application.DataPortability;
using ThymeMe.Application.Settings;
using ThymeMe.Domain.Appearance;
using ThymeMe.Domain.Reporting;
using ThymeMe.Presentation.WinUI.Appearance;
using Windows.System;

namespace ThymeMe.Presentation.WinUI.Settings;

public sealed partial class SettingsPage : UserControl
{
    private bool loading = true;

    public SettingsPage(SettingsViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public SettingsViewModel ViewModel { get; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LoadControls(ViewModel.Current);
        loading = true;
        PaletteList.ItemsSource = ViewModel.Palettes;
        SelectPalette(ViewModel.Current.SelectedPaletteId);
        loading = false;
        UpdateDependentControls();
        UpdateRoundingExample();
        UpdatePalettePreview();
    }

    private void LoadControls(AppSettings settings)
    {
        loading = true;
        DefaultMinutes.Value = settings.DefaultSessionMinutes;
        PomodoroWork.Value = settings.PomodoroWorkMinutes;
        PomodoroBreak.Value = settings.PomodoroBreakMinutes;
        PomodoroBuffer.Value = settings.PomodoroBufferMinutes;
        BreaksBillable.IsOn = settings.PomodoroBreaksBillable;
        Rounding.IsOn = settings.RoundingEnabled;
        RoundingIncrement.Value = settings.RoundingIncrementMinutes;
        Billing.IsOn = settings.BillingEnabled;
        ForegroundTracking.IsOn = settings.ForegroundTrackingEnabled;
        InterruptionHandling.IsOn = settings.InterruptionHandlingEnabled;
        InactivityMinutes.Value = settings.InactivityThresholdMinutes;
        StopIdle.IsOn = settings.StopOnGenericInactivity;
        StopDisplay.IsOn = settings.StopOnDisplayOff;
        StopScreenSaver.IsOn = settings.StopOnScreenSaver;
        StopLock.IsOn = settings.StopOnLock;
        StopSuspend.IsOn = settings.StopOnSuspend;
        StartWindows.IsOn = settings.StartWithWindows;
        Notifications.IsOn = settings.NotificationsEnabled;
        Sound.IsOn = settings.SoundEnabled;
        NavigationWidth.Value = settings.NavigationWidth;
        ChannelWidth.Value = settings.SharedChannelWidth;
        IconScale.Value = settings.IconScale;
        SelectTag(WeekStart, settings.WeekStart.ToString());
        SelectTag(Appearance, settings.AppearanceMode.ToString());
        LightStart.Time = settings.ScheduledLightStart.ToTimeSpan();
        DarkStart.Time = settings.ScheduledDarkStart.ToTimeSpan();
        Retention.Value = settings.RecentlyDeletedRetentionDays;
        UpdateChecks.IsOn = false;
        UpdateDownloads.IsOn = false;
        loading = false;
    }

    private async ValueTask SaveControlsAsync()
    {
        if (loading)
        {
            return;
        }

        AppSettings current = ViewModel.Current;
        if (!TryReadInt(DefaultMinutes, out int defaultMinutes) ||
            !TryReadInt(PomodoroWork, out int pomodoroWork) ||
            !TryReadInt(PomodoroBreak, out int pomodoroBreak) ||
            !TryReadInt(PomodoroBuffer, out int pomodoroBuffer) ||
            !TryReadInt(RoundingIncrement, out int roundingIncrement) ||
            !TryReadInt(InactivityMinutes, out int inactivityMinutes) ||
            !TryReadInt(Retention, out int retention) ||
            !TryReadDouble(NavigationWidth, out double navigationWidth) ||
            !TryReadDouble(ChannelWidth, out double channelWidth) ||
            !TryReadDouble(IconScale, out double iconScale))
        {
            ViewModel.SaveStatus = "Not saved: enter a valid value in every numeric field.";
            return;
        }

        AppSettings settings = current with
        {
            DefaultSessionMinutes = defaultMinutes,
            PomodoroWorkMinutes = pomodoroWork,
            PomodoroBreakMinutes = pomodoroBreak,
            PomodoroBufferMinutes = pomodoroBuffer,
            PomodoroBreaksBillable = BreaksBillable.IsOn,
            RoundingEnabled = Rounding.IsOn,
            RoundingIncrementMinutes = roundingIncrement,
            BillingEnabled = Billing.IsOn,
            ForegroundTrackingEnabled = ForegroundTracking.IsOn,
            InterruptionHandlingEnabled = InterruptionHandling.IsOn,
            InactivityThresholdMinutes = inactivityMinutes,
            StopOnGenericInactivity = StopIdle.IsOn,
            StopOnDisplayOff = StopDisplay.IsOn,
            StopOnScreenSaver = StopScreenSaver.IsOn,
            StopOnLock = StopLock.IsOn,
            StopOnSuspend = StopSuspend.IsOn,
            StartWithWindows = StartWindows.IsOn,
            NotificationsEnabled = Notifications.IsOn,
            SoundEnabled = Sound.IsOn,
            NavigationWidth = navigationWidth,
            SharedChannelWidth = channelWidth,
            IconScale = iconScale,
            WeekStart = ParseTag(WeekStart, current.WeekStart),
            AppearanceMode = ParseTag(Appearance, current.AppearanceMode),
            ScheduledLightStart = TimeOnly.FromTimeSpan(LightStart.Time),
            ScheduledDarkStart = TimeOnly.FromTimeSpan(DarkStart.Time),
            RecentlyDeletedRetentionDays = retention,
            CheckForUpdatesAutomatically = false,
            DownloadUpdatesAutomatically = false,
        };
        if (!await ViewModel.SaveAsync(settings))
        {
            LoadControls(ViewModel.Current);
        }

        UpdateDependentControls();
        UpdateRoundingExample();
        UpdatePalettePreview();
    }

    private async void OnSettingChanged(object sender, SelectionChangedEventArgs e) => await SaveControlsAsync();

    private async void OnSettingToggled(object sender, RoutedEventArgs e) => await SaveControlsAsync();

    private async void OnNumberChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) => await SaveControlsAsync();

    private async void OnTimeChanged(object sender, TimePickerValueChangedEventArgs e) => await SaveControlsAsync();

    private async void OnPaletteSelected(object sender, SelectionChangedEventArgs e)
    {
        UpdatePalettePreview();
        if (loading || PaletteList.SelectedItem is not PaletteListItem selected)
        {
            return;
        }

        Dictionary<string, string> acknowledgements = new(ViewModel.Current.PaletteContrastAcknowledgements, StringComparer.Ordinal);
        PaletteEditorResult assessment = new(selected.Name, selected.Option.Palette.Light, selected.Option.Palette.Dark);
        string signature = SettingsViewModel.PaletteSignature(assessment.Light, assessment.Dark);
        if (!selected.IsBuiltIn && assessment.NeedsContrastAcknowledgement &&
            (!acknowledgements.TryGetValue(selected.Key, out string? acknowledged) || !string.Equals(acknowledged, signature, StringComparison.Ordinal)))
        {
            if (!await ConfirmLowContrastAsync(assessment))
            {
                loading = true;
                SelectPalette(ViewModel.Current.SelectedPaletteId);
                loading = false;
                return;
            }

            acknowledgements[selected.Key] = signature;
        }

        _ = await ViewModel.SaveAsync(ViewModel.Current with
        {
            SelectedPaletteId = selected.Key,
            PaletteContrastAcknowledgements = acknowledgements,
        });
    }

    private void OnPaletteSearchChanged(object sender, TextChangedEventArgs e)
    {
        string query = PaletteSearch.Text.Trim();
        PaletteList.ItemsSource = query.Length == 0
            ? ViewModel.Palettes
            : ViewModel.Palettes.Where(item => item.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)).ToList();
        SelectPalette(ViewModel.Current.SelectedPaletteId);
    }

    private async void OnNewPalette(object sender, RoutedEventArgs e) => await EditPaletteAsync(null);

    private async void OnEditPalette(object sender, RoutedEventArgs e)
    {
        if (PaletteList.SelectedItem is not PaletteListItem { IsBuiltIn: false } selected)
        {
            ViewModel.SaveStatus = "Select a custom palette to edit.";
            return;
        }

        await EditPaletteAsync(selected);
    }

    private async ValueTask EditPaletteAsync(PaletteListItem? existing)
    {
        try
        {
            PaletteEditorResult? result = await PaletteEditorDialog.ShowAsync(XamlRoot, existing?.Option.Palette);
            if (result is null || !await ConfirmLowContrastAsync(result))
            {
                return;
            }

            string? key = await ViewModel.SaveCustomPaletteAsync(existing?.Id, result.Name, result.Light, result.Dark);
            if (key is null)
            {
                return;
            }

            Dictionary<string, string> acknowledgements = new(ViewModel.Current.PaletteContrastAcknowledgements, StringComparer.Ordinal);
            if (result.NeedsContrastAcknowledgement)
            {
                acknowledgements[key] = SettingsViewModel.PaletteSignature(result.Light, result.Dark);
            }
            else
            {
                acknowledgements.Remove(key);
            }

            _ = await ViewModel.SaveAsync(ViewModel.Current with
            {
                SelectedPaletteId = key,
                PaletteContrastAcknowledgements = acknowledgements,
            });
            PaletteList.ItemsSource = ViewModel.Palettes;
            SelectPalette(key);
        }
        catch (Exception exception)
        {
            ViewModel.SaveStatus = $"Palette not saved: {exception.Message}";
        }
    }

    private async ValueTask<bool> ConfirmLowContrastAsync(PaletteEditorResult result)
    {
        if (!result.NeedsContrastAcknowledgement)
        {
            return true;
        }

        ContentDialog warning = new()
        {
            XamlRoot = XamlRoot,
            Title = "Save a low-contrast palette?",
            Content = $"Accent-to-Surface contrast is {result.LightInteractiveContrast:F2}:1 in Light and {result.DarkInteractiveContrast:F2}:1 in Dark. Values below 3:1 may make interactive elements difficult to distinguish.",
            PrimaryButtonText = "Acknowledge and save",
            CloseButtonText = "Keep editing",
            DefaultButton = ContentDialogButton.Close,
        };
        return await warning.ShowAsync() == ContentDialogResult.Primary;
    }

    private async void OnDuplicatePalette(object sender, RoutedEventArgs e)
    {
        if (PaletteList.SelectedItem is PaletteListItem { IsBuiltIn: false } selected)
        {
            string? key = await ViewModel.DuplicatePaletteAsync(selected.Id);
            PaletteList.ItemsSource = ViewModel.Palettes;
            SelectPalette(key);
        }
        else
        {
            ViewModel.SaveStatus = "Select a custom palette to duplicate.";
        }
    }

    private async void OnImportPalette(object sender, RoutedEventArgs e)
    {
        string? key = await ViewModel.ImportPaletteAsync();
        PaletteList.ItemsSource = ViewModel.Palettes;
        SelectPalette(key);
    }

    private async void OnExportPalette(object sender, RoutedEventArgs e)
    {
        if (PaletteList.SelectedItem is PaletteListItem { IsBuiltIn: false } selected)
        {
            await ViewModel.ExportPaletteAsync(selected.Id);
        }
        else
        {
            ViewModel.SaveStatus = "Select a custom palette to export.";
        }
    }

    private async void OnDeletePalette(object sender, RoutedEventArgs e)
    {
        if (PaletteList.SelectedItem is not PaletteListItem { IsBuiltIn: false } selected)
        {
            ViewModel.SaveStatus = "Built-in palettes cannot be deleted.";
            return;
        }

        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = $"Delete {selected.Name}?",
            Content = "This removes the custom palette. Tracked time and organization data are unaffected.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        bool active = string.Equals(ViewModel.Current.SelectedPaletteId, selected.Key, StringComparison.Ordinal);
        if (active && !await ViewModel.SaveAsync(ViewModel.Current with { SelectedPaletteId = "github-default" }))
        {
            return;
        }

        if (await ViewModel.DeletePaletteAsync(selected.Id))
        {
            PaletteList.ItemsSource = ViewModel.Palettes;
            SelectPalette(active ? "github-default" : null);
        }
    }

    private async void OnResetLayout(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = ConfirmDialog(
            "Reset layout?",
            "Navigation width, shared Stream width, and interface scale will return to their defaults.",
            "Reset layout");
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        AppSettings settings = ViewModel.Current with { NavigationWidth = 64, SharedChannelWidth = 184, IconScale = 1 };
        if (await ViewModel.SaveAsync(settings))
        {
            LoadControls(settings);
        }
    }

    private async void OnResetAllSettings(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = ConfirmDialog(
            "Reset all settings?",
            "Preferences return to recommended defaults. Streams, Categories, Projects, Sessions, and other user data are preserved.",
            "Reset settings");
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        AppSettings defaults = AppSettings.RecommendedDefaults with
        {
            WindowX = ViewModel.Current.WindowX,
            WindowY = ViewModel.Current.WindowY,
            WindowWidth = ViewModel.Current.WindowWidth,
            WindowHeight = ViewModel.Current.WindowHeight,
            WindowMaximized = ViewModel.Current.WindowMaximized,
        };
        if (await ViewModel.SaveAsync(defaults))
        {
            LoadControls(defaults);
            SelectPalette(defaults.SelectedPaletteId);
        }
    }

    private void OnRerunWizard(object sender, RoutedEventArgs e) => ViewModel.RequestRerunSetup();

    private async void OnCreateSample(object sender, RoutedEventArgs e) =>
        await ViewModel.CreateSampleAsync(sender is Button { Tag: "Math" });

    private async void OnExportJson(object sender, RoutedEventArgs e) => await ViewModel.ExportJsonAsync(IncludeApplications.IsOn);

    private async void OnExportCsv(object sender, RoutedEventArgs e) => await ViewModel.ExportCsvAsync(IncludeApplications.IsOn);

    private async void OnCreateBackup(object sender, RoutedEventArgs e) => await ViewModel.CreateBackupAsync();

    private async void OnCheckUpdates(object sender, RoutedEventArgs e) => await ViewModel.CheckForUpdatesAsync();

    private async void OnCopyDiagnostics(object sender, RoutedEventArgs e) => await ViewModel.CopyDiagnosticsAsync();

    private async void OnPurgeActivity(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = ConfirmDialog(
            "Delete all application activity?",
            "This permanently removes foreground-application summaries. Sessions are preserved.",
            "Delete");
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.PurgeActivityAsync();
        }
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
                Content = $"{preview.StreamCount} Streams, {preview.CategoryCount} Categories, {preview.ProjectCount} Projects, and {preview.SessionCount} Sessions. Replace is recommended; Merge preserves current data.",
                PrimaryButtonText = "Replace",
                SecondaryButtonText = "Merge",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
            };
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.RestoreAsync(RestoreMode.Replace);
            }
            else if (result == ContentDialogResult.Secondary)
            {
                await ViewModel.RestoreAsync(RestoreMode.Merge);
            }
        }
        catch (Exception exception)
        {
            ViewModel.SaveStatus = exception.Message;
        }
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        string query = SearchBox.Text.Trim();
        foreach (FrameworkElement card in SettingsGrid.Children.OfType<FrameworkElement>())
        {
            string searchable = card.Tag?.ToString() ?? string.Empty;
            card.Visibility = query.Length == 0 || searchable.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.F && InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
        {
            e.Handled = true;
            SearchBox.Focus(FocusState.Keyboard);
            SearchBox.SelectAll();
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        bool narrow = e.NewSize.Width < 860;
        SettingsGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
        SettingsGrid.ColumnDefinitions[1].Width = narrow ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        int index = 0;
        foreach (FrameworkElement card in SettingsGrid.Children.OfType<FrameworkElement>())
        {
            Grid.SetColumn(card, narrow ? 0 : index % 2);
            Grid.SetRow(card, narrow ? index : index / 2);
            index++;
        }
    }

    private void UpdateDependentControls()
    {
        SchedulePanel.Visibility = ParseTag(Appearance, AppearanceMode.FollowWindows) == AppearanceMode.Scheduled
            ? Visibility.Visible
            : Visibility.Collapsed;
        RoundingIncrement.IsEnabled = Rounding.IsOn;
        InactivityMinutes.IsEnabled = InterruptionHandling.IsOn;
        StopIdle.IsEnabled = InterruptionHandling.IsOn;
        StopDisplay.IsEnabled = InterruptionHandling.IsOn;
        StopScreenSaver.IsEnabled = InterruptionHandling.IsOn;
        StopLock.IsEnabled = InterruptionHandling.IsOn;
        StopSuspend.IsEnabled = InterruptionHandling.IsOn;
    }

    private void UpdateRoundingExample()
    {
        if (!TryReadInt(RoundingIncrement, out int increment))
        {
            RoundingExample.Text = "Enter a valid increment to preview rounding.";
            return;
        }

        long sample = TimeSpan.FromMinutes(12).Add(TimeSpan.FromSeconds(34)).Ticks / TimeSpan.TicksPerMillisecond;
        long rounded = RoundingPolicy.Apply(sample, increment, RoundingRule.Nearest).EffectiveMilliseconds;
        RoundingExample.Text = Rounding.IsOn
            ? $"Example: 12:34 rounds to {TimeSpan.FromMilliseconds(rounded):mm\\:ss}."
            : "Rounding is off; reports use exact time by default.";
    }

    private void UpdatePalettePreview()
    {
        PaletteListItem selected = PaletteList.SelectedItem as PaletteListItem
            ?? ViewModel.Palettes.FirstOrDefault(item => string.Equals(item.Key, ViewModel.Current.SelectedPaletteId, StringComparison.Ordinal))
            ?? ViewModel.Palettes.First();
        bool dark = ParseTag(Appearance, ViewModel.Current.AppearanceMode) == AppearanceMode.Dark ||
            (ParseTag(Appearance, ViewModel.Current.AppearanceMode) is AppearanceMode.FollowWindows or AppearanceMode.Scheduled && ActualTheme == ElementTheme.Dark);
        PaletteVariant variant = dark ? selected.Option.Palette.Dark : selected.Option.Palette.Light;
        Windows.UI.Color canvas = AppearanceRuntime.Parse(variant.Canvas);
        Windows.UI.Color surface = AppearanceRuntime.Parse(variant.Surface);
        Windows.UI.Color accent = AppearanceRuntime.Parse(variant.Accent);
        PalettePreviewCanvas.Background = new SolidColorBrush(canvas);
        PalettePreviewCanvas.BorderBrush = new SolidColorBrush(AppearanceRuntime.HighestContrast(canvas));
        PalettePreviewSurface.Background = new SolidColorBrush(surface);
        PalettePreviewText.Foreground = new SolidColorBrush(AppearanceRuntime.HighestContrast(surface));
        PalettePreviewTitle.Foreground = new SolidColorBrush(AppearanceRuntime.HighestContrast(canvas));
        PalettePreviewAccent.Background = new SolidColorBrush(accent);
        PalettePreviewAccent.Foreground = new SolidColorBrush(AppearanceRuntime.HighestContrast(accent));
        double contrast = ContrastPolicy.Ratio(variant.Accent, variant.Surface);
        PaletteContrast.Text = $"{(dark ? "Dark" : "Light")} Accent-to-Surface contrast: {contrast:F2}:1{(contrast < 3 ? " — acknowledgement required for custom palettes" : string.Empty)}";
    }

    private void SelectPalette(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        PaletteList.SelectedItem = PaletteList.Items.OfType<PaletteListItem>()
            .FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.Ordinal));
    }

    private ContentDialog ConfirmDialog(string title, string content, string primaryText) => new()
    {
        XamlRoot = XamlRoot,
        Title = title,
        Content = content,
        PrimaryButtonText = primaryText,
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Close,
    };

    private static bool TryReadInt(NumberBox box, out int value)
    {
        value = 0;
        return !double.IsNaN(box.Value) && !double.IsInfinity(box.Value) &&
            box.Value >= int.MinValue && box.Value <= int.MaxValue &&
            (value = checked((int)box.Value)) == box.Value;
    }

    private static bool TryReadDouble(NumberBox box, out double value)
    {
        value = box.Value;
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private static void SelectTag(ComboBox comboBox, string tag) =>
        comboBox.SelectedItem = comboBox.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), tag, StringComparison.Ordinal));

    private static T ParseTag<T>(ComboBox comboBox, T fallback) where T : struct, Enum =>
        comboBox.SelectedItem is ComboBoxItem item && Enum.TryParse(item.Tag?.ToString(), out T value) ? value : fallback;
}
