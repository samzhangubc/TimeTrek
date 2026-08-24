using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TimeTrek.Application.Reporting;

namespace TimeTrek.Presentation.WinUI.Reporting;

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
}
