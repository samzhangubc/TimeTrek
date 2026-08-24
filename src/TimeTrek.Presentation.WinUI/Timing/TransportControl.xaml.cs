using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace TimeTrek.Presentation.WinUI.Timing;

public sealed partial class TransportControl : UserControl
{
    public TransportControl(TransportViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public TransportViewModel ViewModel { get; }

    public async ValueTask RefreshAsync()
    {
        await ViewModel.RefreshAsync();
        ErrorBar.IsOpen = !string.IsNullOrWhiteSpace(ViewModel.ErrorMessage);
        ErrorBar.Title = ViewModel.ErrorMessage ?? string.Empty;
        CompletionPanel.Visibility = ViewModel.IsCompletionPending ? Visibility.Visible : Visibility.Collapsed;
        ConfirmTransitionButton.Visibility = ViewModel.IsTransitionPending ? Visibility.Visible : Visibility.Collapsed;
        SkipTransitionButton.Visibility = ViewModel.CanSkipInterval ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void OnStart(object sender, RoutedEventArgs e) { await ViewModel.StartAsync(); await RefreshAsync(); }

    private async void OnPause(object sender, RoutedEventArgs e) { await ViewModel.PauseAsync(); await RefreshAsync(); }

    private async void OnResume(object sender, RoutedEventArgs e) { await ViewModel.ResumeAsync(); await RefreshAsync(); }

    private async void OnStop(object sender, RoutedEventArgs e) { await ViewModel.StopAsync(); await RefreshAsync(); }

    private async void OnApplyDuration(object sender, RoutedEventArgs e) { await ViewModel.ApplyDurationAsync(); await RefreshAsync(); }

    private async void OnConfirmTransition(object sender, RoutedEventArgs e) { await ViewModel.ConfirmTransitionAsync(); await RefreshAsync(); }

    private async void OnSkipTransition(object sender, RoutedEventArgs e) { await ViewModel.SkipTransitionAsync(); await RefreshAsync(); }

    private async void OnSaveCompletion(object sender, RoutedEventArgs e)
    {
        ViewModel.CompletionDescription = CompletionText.Text;
        await ViewModel.SaveCompletionAsync();
        CompletionText.Text = string.Empty;
        await RefreshAsync();
    }

    private void OnCompletionKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && !Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
        {
            e.Handled = true;
            OnSaveCompletion(sender, new RoutedEventArgs());
        }
    }
}
