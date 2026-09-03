using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ThymeMe.Presentation.WinUI.Archive;

public sealed partial class ArchivePage : UserControl
{
    public ArchivePage(ArchiveViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public ArchiveViewModel ViewModel { get; }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        bool narrow = e.NewSize.Width < 720;
        HeaderGrid.ColumnDefinitions[1].Width = narrow ? new GridLength(1, GridUnitType.Star) : new GridLength(260);
        Grid.SetRow(SearchBox, narrow ? 1 : 0);
        Grid.SetColumn(SearchBox, narrow ? 0 : 1);
        Grid.SetColumnSpan(SearchBox, narrow ? 2 : 1);
        Grid.SetRow(SearchButton, narrow ? 1 : 0);
        Grid.SetColumn(SearchButton, 2);
    }

    private async void OnSearch(object sender, RoutedEventArgs e)
    {
        ViewModel.Search = SearchBox.Text;
        await ViewModel.LoadAsync();
    }

    private async void OnRestore(object sender, RoutedEventArgs e)
    {
        foreach (ArchivedRow row in ArchiveList.SelectedItems.OfType<ArchivedRow>().ToArray())
        {
            await ViewModel.RestoreAsync(row);
        }
    }
}
