using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TimeTrek.Application.DataPortability;
using TimeTrek.Application.Settings;

namespace TimeTrek.Presentation.WinUI.Settings;

public sealed partial class SettingsPage : UserControl
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public SettingsViewModel ViewModel { get; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AppSettings settings = ViewModel.Current;
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
        UpdateChecks.IsOn = settings.CheckForUpdatesAutomatically;
        UpdateDownloads.IsOn = settings.DownloadUpdatesAutomatically;
    }

    private async void OnSave(object sender, RoutedEventArgs e)
    {
        AppSettings current = ViewModel.Current;
        AppSettings settings = current with
        {
            DefaultSessionMinutes = checked((int)DefaultMinutes.Value),
            PomodoroWorkMinutes = checked((int)PomodoroWork.Value),
            PomodoroBreakMinutes = checked((int)PomodoroBreak.Value),
            PomodoroBufferMinutes = checked((int)PomodoroBuffer.Value),
            PomodoroBreaksBillable = BreaksBillable.IsOn,
            RoundingEnabled = Rounding.IsOn,
            RoundingIncrementMinutes = checked((int)RoundingIncrement.Value),
            BillingEnabled = Billing.IsOn,
            ForegroundTrackingEnabled = ForegroundTracking.IsOn,
            InterruptionHandlingEnabled = InterruptionHandling.IsOn,
            InactivityThresholdMinutes = checked((int)InactivityMinutes.Value),
            StopOnGenericInactivity = StopIdle.IsOn,
            StopOnDisplayOff = StopDisplay.IsOn,
            StopOnScreenSaver = StopScreenSaver.IsOn,
            StopOnLock = StopLock.IsOn,
            StopOnSuspend = StopSuspend.IsOn,
            StartWithWindows = StartWindows.IsOn,
            NotificationsEnabled = Notifications.IsOn,
            SoundEnabled = Sound.IsOn,
            NavigationWidth = NavigationWidth.Value,
            SharedChannelWidth = ChannelWidth.Value,
            IconScale = IconScale.Value,
            WeekStart = ParseTag(WeekStart, current.WeekStart),
            AppearanceMode = ParseTag(Appearance, current.AppearanceMode),
            ScheduledLightStart = TimeOnly.FromTimeSpan(LightStart.Time),
            ScheduledDarkStart = TimeOnly.FromTimeSpan(DarkStart.Time),
            RecentlyDeletedRetentionDays = checked((int)Retention.Value),
            CheckForUpdatesAutomatically = UpdateChecks.IsOn,
            DownloadUpdatesAutomatically = UpdateChecks.IsOn && UpdateDownloads.IsOn,
        };
        await ViewModel.SaveAsync(settings);
    }

    private void OnResetLayout(object sender, RoutedEventArgs e)
    {
        NavigationWidth.Value = 64;
        ChannelWidth.Value = 184;
        IconScale.Value = 1;
    }

    private static void SelectTag(ComboBox comboBox, string tag)
    {
        comboBox.SelectedItem = comboBox.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), tag, StringComparison.Ordinal));
    }

    private static T ParseTag<T>(ComboBox comboBox, T fallback) where T : struct, Enum =>
        comboBox.SelectedItem is ComboBoxItem item && Enum.TryParse(item.Tag?.ToString(), out T value) ? value : fallback;

    private async void OnExportJson(object sender, RoutedEventArgs e) => await ViewModel.ExportJsonAsync();

    private async void OnExportCsv(object sender, RoutedEventArgs e) => await ViewModel.ExportCsvAsync();

    private async void OnCreateBackup(object sender, RoutedEventArgs e) => await ViewModel.CreateBackupAsync();

    private async void OnCheckUpdates(object sender, RoutedEventArgs e) => await ViewModel.CheckForUpdatesAsync();

    private async void OnCopyDiagnostics(object sender, RoutedEventArgs e) => await ViewModel.CopyDiagnosticsAsync();

    private async void OnPurgeActivity(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = "Delete all application activity?",
            Content = "This permanently removes foreground-application summaries. Sessions are preserved.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };
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
            string searchable = card is Border { Child: StackPanel panel }
                ? string.Join(' ', panel.Children.OfType<TextBlock>().Select(item => item.Text))
                : string.Empty;
            card.Visibility = query.Length == 0 || searchable.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}
