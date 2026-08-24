using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TimeTrek.Presentation.WinUI.Archive;

public sealed partial class ArchivePage : UserControl
{
    public ArchivePage(ArchiveViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public ArchiveViewModel ViewModel { get; }

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
