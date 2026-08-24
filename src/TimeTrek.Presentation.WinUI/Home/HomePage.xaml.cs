using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TimeTrek.Presentation.WinUI.Timing;

namespace TimeTrek.Presentation.WinUI.Home;

public sealed partial class HomePage : UserControl
{
    private readonly TransportViewModel transport;

    public HomePage(HomeViewModel viewModel, TransportViewModel transport)
    {
        ViewModel = viewModel;
        this.transport = transport;
        InitializeComponent();
    }

    public HomeViewModel ViewModel { get; }

    private async void OnAddStream(object sender, RoutedEventArgs e)
    {
        ViewModel.NewStreamName = NewStreamName.Text;
        await ViewModel.CreateStreamCommand.ExecuteAsync(null);
        NewStreamName.Text = ViewModel.NewStreamName;
        ErrorBar.IsOpen = !string.IsNullOrWhiteSpace(ViewModel.ErrorMessage);
        ErrorBar.Title = ViewModel.ErrorMessage ?? string.Empty;
    }

    private async void OnStartTimer(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Guid streamId })
        {
            return;
        }

        TextBox duration = new()
        {
            Header = "Required duration",
            Text = transport.DurationText,
            PlaceholderText = "25, 1.5h, 1h 30m, 90m, or 01:30",
        };
        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = "Start Session",
            Content = duration,
            PrimaryButtonText = "Start",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            transport.SelectedStreamId = streamId;
            transport.DurationText = duration.Text;
            await transport.StartAsync();
            ErrorBar.IsOpen = !string.IsNullOrWhiteSpace(transport.ErrorMessage);
            ErrorBar.Title = transport.ErrorMessage ?? string.Empty;
        }
    }

    private async void OnAddSession(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Guid streamId })
        {
            await ShowHistoryEntryAsync(streamId, adjustment: false);
        }
    }

    private async void OnAdjust(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Guid streamId })
        {
            await ShowHistoryEntryAsync(streamId, adjustment: true);
        }
    }

    private async Task ShowHistoryEntryAsync(Guid streamId, bool adjustment)
    {
        TextBox duration = new()
        {
            Header = adjustment ? "Time to subtract" : "Duration",
            Text = "25",
            PlaceholderText = "25, 1.5h, 1h 30m, 90m, or 01:30",
        };
        TextBox description = new() { Header = "Description (optional)", MaxLength = 4000 };
        StackPanel content = new() { Spacing = 10 };
        content.Children.Add(duration);
        content.Children.Add(description);
        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = adjustment ? "Subtract time" : "Add Session",
            Content = content,
            PrimaryButtonText = adjustment ? "Subtract" : "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            string? error = adjustment
                ? await ViewModel.AddAdjustmentAsync(streamId, duration.Text, description.Text)
                : await ViewModel.AddManualAsync(streamId, duration.Text, description.Text);
            ErrorBar.IsOpen = error is not null;
            ErrorBar.Title = error ?? string.Empty;
        }
    }

    private async void OnAddCategory(object sender, RoutedEventArgs e) => await ShowOrganizationDialogAsync(category: true);

    private async void OnAddProject(object sender, RoutedEventArgs e) => await ShowOrganizationDialogAsync(category: false);

    private async Task ShowOrganizationDialogAsync(bool category)
    {
        TextBox name = new() { Header = category ? "Category name" : "Project name", MaxLength = 200 };
        ComboBox owner = new()
        {
            Header = category ? "Scope" : "Owning Stream",
            ItemsSource = ViewModel.Streams,
            DisplayMemberPath = nameof(StreamCard.Name),
            SelectedValuePath = nameof(StreamCard.Id),
            PlaceholderText = category ? "Global Category" : "Standalone Project",
        };
        ToggleSwitch billable = new() { Header = "Billable by default", Visibility = category ? Visibility.Visible : Visibility.Collapsed };
        NumberBox rate = new() { Header = "Hourly rate in minor units", Minimum = 0, Maximum = 1_000_000_000, Visibility = category ? Visibility.Visible : Visibility.Collapsed };
        TextBox currency = new() { Header = "ISO currency code", MaxLength = 3, Visibility = category ? Visibility.Visible : Visibility.Collapsed };
        StackPanel content = new() { Spacing = 8 };
        content.Children.Add(name);
        content.Children.Add(owner);
        content.Children.Add(billable);
        content.Children.Add(rate);
        content.Children.Add(currency);
        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = category ? "Add Category" : "Add Project",
            Content = content,
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            Guid? streamId = owner.SelectedValue is Guid id ? id : null;
            string? error = category
                ? await ViewModel.CreateCategoryAsync(
                    name.Text, streamId, billable.IsOn,
                    billable.IsOn && !double.IsNaN(rate.Value) ? checked((long)rate.Value) : null,
                    billable.IsOn ? currency.Text : null)
                : await ViewModel.CreateProjectAsync(name.Text, streamId);
            ErrorBar.IsOpen = error is not null;
            ErrorBar.Title = error ?? string.Empty;
        }
    }

    private async void OnArchive(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Guid streamId })
        {
            return;
        }

        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = "Archive this Stream?",
            Content = "Its historical Sessions remain intact and it can be restored from Archive.",
            PrimaryButtonText = "Archive",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.ArchiveStreamAsync(streamId);
        }
    }
}
