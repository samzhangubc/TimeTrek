using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TimeTrek.Domain.Timing;
using TimeTrek.Presentation.WinUI.Timing;

namespace TimeTrek.Presentation.WinUI.History;

public sealed partial class HistoryPage : UserControl
{
    private readonly TransportViewModel transport;

    public HistoryPage(HistoryViewModel viewModel, TransportViewModel transport)
    {
        ViewModel = viewModel;
        this.transport = transport;
        InitializeComponent();
    }

    public HistoryViewModel ViewModel { get; }

    private async void OnSearch(object sender, RoutedEventArgs e)
    {
        ViewModel.Search = SearchBox.Text;
        await ViewModel.LoadAsync();
    }

    private async void OnAddSession(object sender, RoutedEventArgs e) => await ShowEntryAsync(adjustment: false);

    private async void OnAddAdjustment(object sender, RoutedEventArgs e) => await ShowEntryAsync(adjustment: true);

    private async Task ShowEntryAsync(bool adjustment)
    {
        TextBox duration = new() { Header = adjustment ? "Time to subtract" : "Duration", Text = "25" };
        TextBox description = new() { Header = "Description (optional)", MaxLength = 4000 };
        StackPanel content = new() { Spacing = 10 };
        content.Children.Add(duration);
        content.Children.Add(description);
        ContentDialog dialog = CreateDialog(adjustment ? "Subtract time" : "Add Session", content, adjustment ? "Subtract" : "Add");
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            string? error = adjustment
                ? await ViewModel.AddAdjustmentAsync(duration.Text, description.Text)
                : await ViewModel.AddManualAsync(duration.Text, description.Text);
            if (error is not null)
            {
                await ShowMessageAsync("Could not save", error);
            }
        }
    }

    private async void OnContinue(object sender, RoutedEventArgs e)
    {
        if (HistoryList.SelectedItem is not HistoryRow { IsDeleted: false } row)
        {
            return;
        }

        CompletedSession? session = await ViewModel.GetSessionAsync(row.Id);
        if (session is not null)
        {
            await transport.StartFromSessionAsync(session);
            if (transport.ErrorMessage is not null)
            {
                await ShowMessageAsync("Could not continue", transport.ErrorMessage);
            }
        }
    }

    private async void OnEditDescription(object sender, RoutedEventArgs e)
    {
        if (HistoryList.SelectedItem is not HistoryRow { IsDeleted: false } row)
        {
            return;
        }

        TextBox description = new() { Text = row.Description, AcceptsReturn = true, MaxLength = 4000 };
        ContentDialog dialog = CreateDialog("Edit description", description, "Save");
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            string? error = await ViewModel.UpdateDescriptionAsync(row.Id, description.Text);
            if (error is not null)
            {
                await ShowMessageAsync("Could not save", error);
            }
        }
    }

    private async void OnDeleteOrRestore(object sender, RoutedEventArgs e)
    {
        HistoryRow[] rows = HistoryList.SelectedItems.OfType<HistoryRow>().ToArray();
        if (rows.Length == 0)
        {
            return;
        }

        ContentDialog confirmation = CreateDialog(
            rows.All(row => row.IsDeleted) ? "Restore selected Sessions?" : "Move selected Sessions to Recently Deleted?",
            "This action preserves Sessions until the configured purge deadline.",
            rows.All(row => row.IsDeleted) ? "Restore" : "Continue");
        if (await confirmation.ShowAsync() == ContentDialogResult.Primary)
        {
            foreach (HistoryRow row in rows)
            {
                await ViewModel.DeleteOrRestoreAsync(row);
            }
        }
    }

    private ContentDialog CreateDialog(string title, object content, string primaryText) => new()
    {
        XamlRoot = XamlRoot,
        Title = title,
        Content = content,
        PrimaryButtonText = primaryText,
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary,
    };

    private async Task ShowMessageAsync(string title, string message)
    {
        ContentDialog dialog = CreateDialog(title, message, "OK");
        dialog.CloseButtonText = string.Empty;
        await dialog.ShowAsync();
    }
}
