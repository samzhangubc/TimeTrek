using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using TimeTrek.Presentation.WinUI.Shell;
using Windows.Graphics;

namespace TimeTrek.App;

public sealed partial class MainWindow : Window
{
    private readonly Func<ValueTask<bool>> requestClose;
    private bool closeAllowed;

    public MainWindow(AppShellViewModel viewModel, Func<ValueTask<bool>> requestClose)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        this.requestClose = requestClose ?? throw new ArgumentNullException(nameof(requestClose));
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));
        AppWindow.Resize(new SizeInt32(1280, 800));
        AppWindow.Closing += OnAppWindowClosing;

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMinimizable = true;
            presenter.IsMaximizable = true;
            presenter.IsResizable = true;
        }

        ShellHost.Content = new AppShell(viewModel);
    }

    public void Restore()
    {
        AppWindow.Show();
        Activate();
    }

    public void AllowCloseAndClose()
    {
        closeAllowed = true;
        Close();
    }

    private async void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (closeAllowed)
        {
            sender.Closing -= OnAppWindowClosing;
            return;
        }

        args.Cancel = true;
        if (await requestClose())
        {
            AllowCloseAndClose();
        }
        else
        {
            AppWindow.Hide();
        }
    }
}
