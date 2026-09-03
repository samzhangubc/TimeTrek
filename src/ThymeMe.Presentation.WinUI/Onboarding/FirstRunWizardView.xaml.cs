using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ThymeMe.Application.DataPortability;
using ThymeMe.Application.Settings;
using ThymeMe.Domain.Appearance;
using ThymeMe.Presentation.WinUI.Appearance;
using ThymeMe.Presentation.WinUI.Settings;
using ThymeMe.Presentation.WinUI.Shell;

namespace ThymeMe.Presentation.WinUI.Onboarding;

public sealed partial class FirstRunWizardView : UserControl
{
    private readonly FrameworkElement[] steps;
    private int step;
    private bool completing;

    public FirstRunWizardView(AppShellViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        steps = [WelcomeStep, OrganizationStep, TimingStep, AppearanceStep, AdvancedStep, FinishStep];
        Loaded += OnLoaded;
    }

    public AppShellViewModel ViewModel { get; }

    public event EventHandler? SetupCompleted;

    public void Restart()
    {
        ErrorBar.IsOpen = false;
        ShowStep(0);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        DefaultMinutes.Value = ViewModel.Wizard.DefaultSessionMinutes;
        WorkMinutes.Value = ViewModel.Wizard.PomodoroWorkMinutes;
        BreakMinutes.Value = ViewModel.Wizard.PomodoroBreakMinutes;
        InactivityMinutes.Value = ViewModel.Wizard.InactivityThresholdMinutes;
        SelectTag(WeekStart, ViewModel.Wizard.WeekStart.ToString());
        SelectTag(Appearance, ViewModel.Wizard.AppearanceMode.ToString());
        WizardPalette.ItemsSource = ViewModel.Settings.Palettes;
        WizardPalette.SelectedItem = ViewModel.Settings.Palettes.FirstOrDefault(item =>
            string.Equals(item.Key, ViewModel.Wizard.SelectedPaletteId, StringComparison.Ordinal));
        UpdateWizardPalettePreview();
        ShowStep(0);
    }

    private async void OnUseRecommended(object sender, RoutedEventArgs e)
    {
        await ViewModel.UseRecommendedDefaultsCommand.ExecuteAsync(null);
        CompleteIfReady();
    }

    private void OnBegin(object sender, RoutedEventArgs e) => ShowStep(1);

    private async void OnRestore(object sender, RoutedEventArgs e)
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
                Title = "Restore this backup?",
                Content = $"This backup contains {preview.StreamCount} Streams and {preview.SessionCount} Sessions. Your current setup will be replaced.",
                PrimaryButtonText = "Restore",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await ViewModel.RestoreAsync(RestoreMode.Replace);
                CompleteIfReady();
            }
        }
        catch (Exception exception)
        {
            ShowError(exception.Message);
        }
    }

    private void OnBack(object sender, RoutedEventArgs e) => ShowStep(Math.Max(1, step - 1));

    private async void OnNext(object sender, RoutedEventArgs e)
    {
        if (!TryApplyFields())
        {
            return;
        }
        try
        {
            await ViewModel.Wizard.SaveProgressAsync();
            ShowStep(Math.Min(5, step + 1));
        }
        catch (Exception exception)
        {
            ShowError(exception.Message);
        }
    }

    private async void OnSkip(object sender, RoutedEventArgs e)
    {
        if (!TryApplyFields())
        {
            return;
        }
        await ViewModel.Wizard.SaveProgressAsync();
        ShowStep(5);
    }

    private async void OnFinish(object sender, RoutedEventArgs e)
    {
        if (completing || !TryApplyFields())
        {
            return;
        }

        completing = true;
        FinishButton.IsEnabled = false;
        ErrorBar.IsOpen = false;
        try
        {
            await ViewModel.CompleteStepByStepCommand.ExecuteAsync(null);
            CompleteIfReady();
        }
        catch (Exception exception)
        {
            ShowError(exception.Message);
        }
        finally
        {
            completing = false;
            FinishButton.IsEnabled = true;
        }
    }

    private void ShowStep(int value)
    {
        step = value;
        for (int index = 0; index < steps.Length; index++)
        {
            steps[index].Visibility = index == step ? Visibility.Visible : Visibility.Collapsed;
        }

        string[] titles = ["Your time stays yours", "Optional examples", "Timing defaults", "Look and startup", "Optional features", "Setup complete"];
        StepTitle.Text = titles[step];
        Progress.Value = step;
        Progress.Visibility = step == 0 ? Visibility.Collapsed : Visibility.Visible;
        BackButton.Visibility = step is > 1 and < 6 ? Visibility.Visible : Visibility.Collapsed;
        NextButton.Visibility = step is >= 1 and < 5 ? Visibility.Visible : Visibility.Collapsed;
        SkipButton.Visibility = step == 4 ? Visibility.Visible : Visibility.Collapsed;
        FinishButton.Visibility = step == 5 ? Visibility.Visible : Visibility.Collapsed;
    }

    private bool TryApplyFields()
    {
        if (!TryReadInt(DefaultMinutes, out int defaultMinutes) ||
            !TryReadInt(WorkMinutes, out int workMinutes) ||
            !TryReadInt(BreakMinutes, out int breakMinutes) ||
            !TryReadInt(InactivityMinutes, out int inactivityMinutes))
        {
            ShowError("Enter a valid whole number in every numeric field.");
            return false;
        }

        ViewModel.Wizard.DefaultSessionMinutes = defaultMinutes;
        ViewModel.Wizard.PomodoroWorkMinutes = workMinutes;
        ViewModel.Wizard.PomodoroBreakMinutes = breakMinutes;
        ViewModel.Wizard.InactivityThresholdMinutes = inactivityMinutes;
        ViewModel.Wizard.WeekStart = ParseTag(WeekStart, WeekStartDay.WindowsDefault);
        ViewModel.Wizard.AppearanceMode = ParseTag(Appearance, AppearanceMode.FollowWindows);
        ViewModel.Wizard.SelectedPaletteId = WizardPalette.SelectedItem is PaletteListItem palette
            ? palette.Key
            : "github-default";
        return true;
    }

    private void OnWizardPaletteChanged(object sender, SelectionChangedEventArgs e) => UpdateWizardPalettePreview();

    private async void OnWizardNewPalette(object sender, RoutedEventArgs e)
    {
        try
        {
            PaletteEditorResult? result = await PaletteEditorDialog.ShowAsync(XamlRoot);
            if (result is null)
            {
                return;
            }

            if (result.NeedsContrastAcknowledgement)
            {
                ContentDialog warning = new()
                {
                    XamlRoot = XamlRoot,
                    Title = "Use a low-contrast palette?",
                    Content = $"Accent-to-Surface contrast is {result.LightInteractiveContrast:F2}:1 in Light and {result.DarkInteractiveContrast:F2}:1 in Dark.",
                    PrimaryButtonText = "Acknowledge and save",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                };
                if (await warning.ShowAsync() != ContentDialogResult.Primary)
                {
                    return;
                }
            }

            string? key = await ViewModel.Settings.SaveCustomPaletteAsync(null, result.Name, result.Light, result.Dark);
            if (key is null)
            {
                ShowError(ViewModel.Settings.SaveStatus);
                return;
            }

            if (result.NeedsContrastAcknowledgement)
            {
                ViewModel.Wizard.PaletteContrastAcknowledgements[key] =
                    SettingsViewModel.PaletteSignature(result.Light, result.Dark);
            }

            WizardPalette.ItemsSource = ViewModel.Settings.Palettes;
            WizardPalette.SelectedItem = ViewModel.Settings.Palettes.First(item => item.Key == key);
        }
        catch (Exception exception)
        {
            ShowError(exception.Message);
        }
    }

    private void UpdateWizardPalettePreview()
    {
        if (WizardPalette.SelectedItem is not PaletteListItem palette)
        {
            WizardPalettePreview.Text = string.Empty;
            return;
        }

        WizardPalettePreview.Text = $"Light  {palette.LightSwatch}\nDark   {palette.DarkSwatch}";
    }

    private static bool TryReadInt(NumberBox box, out int value)
    {
        value = 0;
        return !double.IsNaN(box.Value) && !double.IsInfinity(box.Value) &&
            box.Value >= int.MinValue && box.Value <= int.MaxValue &&
            (value = checked((int)box.Value)) == box.Value;
    }

    private void CompleteIfReady()
    {
        if (ViewModel.IsSetupComplete)
        {
            SetupCompleted?.Invoke(this, EventArgs.Empty);
        }
        else if (!string.IsNullOrWhiteSpace(ViewModel.ErrorMessage))
        {
            ShowError(ViewModel.ErrorMessage);
        }
    }

    private void ShowError(string? message)
    {
        ErrorBar.Title = message ?? "Thyme-Me could not save this step.";
        ErrorBar.IsOpen = true;
    }

    private static void SelectTag(ComboBox comboBox, string tag) =>
        comboBox.SelectedItem = comboBox.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), tag, StringComparison.Ordinal));

    private static T ParseTag<T>(ComboBox comboBox, T fallback) where T : struct, Enum =>
        comboBox.SelectedItem is ComboBoxItem item && Enum.TryParse(item.Tag?.ToString(), out T value) ? value : fallback;
}
