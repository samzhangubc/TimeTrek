using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ThymeMe.Application.Reporting;

namespace ThymeMe.Presentation.WinUI.Reporting;

public sealed partial class StatsPage : UserControl
{
    public StatsPage(StatsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public StatsViewModel ViewModel { get; }

    private async void OnBasis(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string value } && Enum.TryParse(value, out ReportingBasis basis))
        {
            ViewModel.Basis = basis;
            await ViewModel.LoadAsync();
        }
    }

    private async void OnRange(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string value } || !Enum.TryParse(value, out StatsRangePreset preset))
        {
            return;
        }

        if (preset == StatsRangePreset.Custom)
        {
            DatePicker start = new() { Header = "From", Date = ViewModel.RangeStart };
            DatePicker end = new() { Header = "Through", Date = ViewModel.RangeEnd };
            StackPanel content = new() { Spacing = 8 };
            content.Children.Add(start); content.Children.Add(end);
            ContentDialog dialog = new()
            {
                XamlRoot = XamlRoot,
                Title = "Choose a date range",
                Content = content,
                PrimaryButtonText = "Show range",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            {
                return;
            }

            ViewModel.SetCustomRange(start.Date.Date, end.Date.Date.AddDays(1).AddTicks(-1));
        }
        else
        {
            ViewModel.SetPreset(preset);
        }

        await ViewModel.LoadAsync();
    }

    private async void OnMoveRange(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string value } && int.TryParse(value, out int direction))
        {
            ViewModel.MoveRange(direction);
            await ViewModel.LoadAsync();
        }
    }

    private async void OnView(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string value } && Enum.TryParse(value, out StatsViewMode mode))
        {
            ViewModel.ViewMode = mode;
            await ViewModel.LoadAsync();
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        bool narrow = e.NewSize.Width < 900;
        LowerGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
        LowerGrid.ColumnDefinitions[1].Width = narrow ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        Grid.SetRow(StreamRankings, 0); Grid.SetColumn(StreamRankings, 0);
        Grid.SetRow(ProjectRankings, narrow ? 1 : 0); Grid.SetColumn(ProjectRankings, narrow ? 0 : 1);
    }
}
