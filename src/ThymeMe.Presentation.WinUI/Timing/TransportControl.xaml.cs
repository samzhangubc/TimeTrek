using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace ThymeMe.Presentation.WinUI.Timing;

public sealed partial class TransportControl : UserControl
{
    private bool completionWasVisible;
    private bool savingCompletion;

    public TransportControl(TransportViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        PomodoroWork.Value = ViewModel.PomodoroWorkMinutes;
        PomodoroBreak.Value = ViewModel.PomodoroBreakMinutes;
    }

    public TransportViewModel ViewModel { get; }

    public async ValueTask RefreshAsync()
    {
        await ViewModel.RefreshAsync();
        ErrorBar.IsOpen = !string.IsNullOrWhiteSpace(ViewModel.ErrorMessage);
        ErrorBar.Title = ViewModel.ErrorMessage ?? string.Empty;
        bool completionVisible = ViewModel.IsCompletionPending;
        CompletionPanel.Visibility = completionVisible ? Visibility.Visible : Visibility.Collapsed;
        ConfirmTransitionButton.Visibility = ViewModel.IsTransitionPending ? Visibility.Visible : Visibility.Collapsed;
        SkipTransitionButton.Visibility = ViewModel.CanSkipInterval ? Visibility.Visible : Visibility.Collapsed;
        StartButton.IsEnabled = ViewModel.CanStart;
        PauseButton.IsEnabled = ViewModel.CanPause;
        ResumeButton.IsEnabled = ViewModel.CanResume;
        StopButton.IsEnabled = ViewModel.CanStop;
        ApplyDurationButton.IsEnabled = ViewModel.CanApplyDuration;
        StreamPicker.IsEnabled = ViewModel.CanStart;
        SaveCompletionButton.IsEnabled = !savingCompletion;
        if (completionVisible && !completionWasVisible)
        {
            CompletionText.Focus(FocusState.Programmatic);
            CompletionText.SelectAll();
        }

        completionWasVisible = completionVisible;
    }

    private async void OnStart(object sender, RoutedEventArgs e)
    {
        ViewModel.PomodoroWorkMinutes = checked((int)PomodoroWork.Value);
        ViewModel.PomodoroBreakMinutes = checked((int)PomodoroBreak.Value);
        await ViewModel.StartAsync();
        await RefreshAsync();
    }

    private async void OnPause(object sender, RoutedEventArgs e) { await ViewModel.PauseAsync(); await RefreshAsync(); }

    private async void OnResume(object sender, RoutedEventArgs e) { await ViewModel.ResumeAsync(); await RefreshAsync(); }

    private async void OnStop(object sender, RoutedEventArgs e) { await ViewModel.StopAsync(); await RefreshAsync(); }

    private async void OnApplyDuration(object sender, RoutedEventArgs e) { await ViewModel.ApplyDurationAsync(); await RefreshAsync(); }

    private async void OnConfirmTransition(object sender, RoutedEventArgs e) { await ViewModel.ConfirmTransitionAsync(); await RefreshAsync(); }

    private async void OnSkipTransition(object sender, RoutedEventArgs e) { await ViewModel.SkipTransitionAsync(); await RefreshAsync(); }

    private async void OnSaveCompletion(object sender, RoutedEventArgs e)
    {
        if (savingCompletion)
        {
            return;
        }

        savingCompletion = true;
        SaveCompletionButton.IsEnabled = false;
        ViewModel.CompletionDescription = CompletionText.Text;
        try
        {
            await ViewModel.SaveCompletionAsync();
            if (string.IsNullOrWhiteSpace(ViewModel.ErrorMessage))
            {
                CompletionText.Text = string.Empty;
            }
        }
        finally
        {
            savingCompletion = false;
            await RefreshAsync();
        }
    }

    private void OnCompletionKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && !Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
        {
            e.Handled = true;
            OnSaveCompletion(sender, new RoutedEventArgs());
        }
    }

    private void OnDurationGotFocus(object sender, RoutedEventArgs e) => DurationTextBox.SelectAll();

    private async void OnChooseCategories(object sender, RoutedEventArgs e)
    {
        ListView list = new()
        {
            ItemsSource = ViewModel.Categories,
            DisplayMemberPath = nameof(AssociationOption.Name),
            SelectionMode = ListViewSelectionMode.Multiple,
            MaxHeight = 320,
        };
        list.Loaded += (_, _) =>
        {
            foreach (AssociationOption option in ViewModel.Categories.Where(item => ViewModel.SelectedCategoryIds.Contains(item.Id)))
            {
                list.SelectedItems.Add(option);
            }
        };
        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = "Categories for this Session",
            Content = list,
            PrimaryButtonText = "Use selected",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            ViewModel.SelectedCategoryIds.Clear();
            ViewModel.SelectedCategoryIds.UnionWith(list.SelectedItems.OfType<AssociationOption>().Select(item => item.Id));
            ViewModel.RefreshCategorySummary();
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        bool narrow = e.NewSize.Width < 1120;
        bool compact = e.NewSize.Width < 720;
        if (!narrow)
        {
            ButtonsArea.Orientation = Orientation.Horizontal;
            CompletionGrid.ColumnDefinitions[1].Width = GridLength.Auto;
            Grid.SetRow(SaveCompletionButton, 0);
            Grid.SetColumn(SaveCompletionButton, 1);
            double[] widths = [150, 160, 180, 130, 170, 0];
            for (int index = 0; index < TransportGrid.ColumnDefinitions.Count; index++)
            {
                TransportGrid.ColumnDefinitions[index].Width = index switch
                {
                    5 => GridLength.Auto,
                    _ => new GridLength(widths[index]),
                };
            }

            Place(TimerArea, 0, 0); Place(StreamPicker, 0, 1); Place(AssociationArea, 0, 2);
            Place(DurationArea, 0, 3); Place(PomodoroArea, 0, 4); Place(ButtonsArea, 0, 5);
            Place(ErrorBar, 1, 0); Grid.SetColumnSpan(ErrorBar, 6);
            return;
        }

        if (compact)
        {
            TransportGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            for (int index = 1; index < TransportGrid.ColumnDefinitions.Count; index++)
            {
                TransportGrid.ColumnDefinitions[index].Width = new GridLength(0);
            }

            ButtonsArea.Orientation = Orientation.Vertical;
            CompletionGrid.ColumnDefinitions[1].Width = new GridLength(0);
            Grid.SetRow(SaveCompletionButton, 1);
            Grid.SetColumn(SaveCompletionButton, 0);
            Place(TimerArea, 0, 0); Place(StreamPicker, 1, 0); Place(AssociationArea, 2, 0);
            Place(DurationArea, 3, 0); Place(PomodoroArea, 4, 0); Place(ButtonsArea, 5, 0);
            Place(ErrorBar, 6, 0); Grid.SetColumnSpan(ErrorBar, 1);
            return;
        }

        TransportGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
        TransportGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
        for (int index = 2; index < TransportGrid.ColumnDefinitions.Count; index++)
        {
            TransportGrid.ColumnDefinitions[index].Width = new GridLength(0);
        }

        ButtonsArea.Orientation = Orientation.Horizontal;
        CompletionGrid.ColumnDefinitions[1].Width = GridLength.Auto;
        Grid.SetRow(SaveCompletionButton, 0);
        Grid.SetColumn(SaveCompletionButton, 1);
        Place(TimerArea, 0, 0); Place(ButtonsArea, 0, 1);
        Place(StreamPicker, 1, 0); Place(AssociationArea, 1, 1);
        Place(DurationArea, 2, 0); Place(PomodoroArea, 2, 1);
        Place(ErrorBar, 3, 0); Grid.SetColumnSpan(ErrorBar, 2);
    }

    private static void Place(FrameworkElement element, int row, int column)
    {
        Grid.SetRow(element, row);
        Grid.SetColumn(element, column);
        Grid.SetColumnSpan(element, 1);
    }
}
