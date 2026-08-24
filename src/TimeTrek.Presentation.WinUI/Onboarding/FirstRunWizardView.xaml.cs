using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TimeTrek.Application.DataPortability;
using TimeTrek.Application.Settings;
using TimeTrek.Presentation.WinUI.Shell;

namespace TimeTrek.Presentation.WinUI.Onboarding;

public sealed partial class FirstRunWizardView : UserControl
{
    private readonly FrameworkElement[] steps;
    private int step;

    public FirstRunWizardView(AppShellViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        steps = [WelcomeStep, OrganizationStep, TimingStep, AppearanceStep, AdvancedStep, FinishStep];
        Loaded += OnLoaded;
    }

    public AppShellViewModel ViewModel { get; }

    public event EventHandler? SetupCompleted;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        DefaultMinutes.Value = ViewModel.Wizard.DefaultSessionMinutes;
        WorkMinutes.Value = ViewModel.Wizard.PomodoroWorkMinutes;
        BreakMinutes.Value = ViewModel.Wizard.PomodoroBreakMinutes;
        InactivityMinutes.Value = ViewModel.Wizard.InactivityThresholdMinutes;
        SelectTag(WeekStart, ViewModel.Wizard.WeekStart.ToString());
        SelectTag(Appearance, ViewModel.Wizard.AppearanceMode.ToString());
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
        ApplyFields();
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
        ApplyFields();
        await ViewModel.Wizard.SaveProgressAsync();
        ShowStep(5);
    }

    private async void OnFinish(object sender, RoutedEventArgs e)
    {
        ApplyFields();
        await ViewModel.CompleteStepByStepCommand.ExecuteAsync(null);
        CompleteIfReady();
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

    private void ApplyFields()
    {
        ViewModel.Wizard.DefaultSessionMinutes = checked((int)DefaultMinutes.Value);
        ViewModel.Wizard.PomodoroWorkMinutes = checked((int)WorkMinutes.Value);
        ViewModel.Wizard.PomodoroBreakMinutes = checked((int)BreakMinutes.Value);
        ViewModel.Wizard.InactivityThresholdMinutes = checked((int)InactivityMinutes.Value);
        ViewModel.Wizard.WeekStart = ParseTag(WeekStart, WeekStartDay.WindowsDefault);
        ViewModel.Wizard.AppearanceMode = ParseTag(Appearance, AppearanceMode.FollowWindows);
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
        ErrorBar.Title = message ?? "TimeTrek could not save this step.";
        ErrorBar.IsOpen = true;
    }

    private static void SelectTag(ComboBox comboBox, string tag) =>
        comboBox.SelectedItem = comboBox.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), tag, StringComparison.Ordinal));

    private static T ParseTag<T>(ComboBox comboBox, T fallback) where T : struct, Enum =>
        comboBox.SelectedItem is ComboBoxItem item && Enum.TryParse(item.Tag?.ToString(), out T value) ? value : fallback;
}
