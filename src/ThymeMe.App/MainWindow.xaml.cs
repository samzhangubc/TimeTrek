using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using ThymeMe.Application.Settings;
using ThymeMe.Presentation.WinUI.Shell;
using Windows.Graphics;

namespace ThymeMe.App;

public sealed partial class MainWindow : Window
{
    private readonly Func<ValueTask<bool>> requestClose;
    private readonly IAppSettingsStore settingsStore;
    private bool closeAllowed;
    private RectInt32 restoredBounds;
    private bool isMaximized;

    public MainWindow(
        AppShellViewModel viewModel,
        IAppSettingsStore settingsStore,
        Func<ValueTask<bool>> requestClose)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        this.requestClose = requestClose ?? throw new ArgumentNullException(nameof(requestClose));
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));
        AppSettings settings = viewModel.Settings.Current;
        restoredBounds = GetRestoredBounds(settings);
        isMaximized = settings.WindowMaximized;
        AppWindow.MoveAndResize(restoredBounds);

        AppWindow.Closing += OnAppWindowClosing;
        AppWindow.Changed += OnAppWindowChanged;

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMinimizable = true;
            presenter.IsMaximizable = true;
            presenter.IsResizable = true;
            presenter.PreferredMinimumWidth = 960;
            presenter.PreferredMinimumHeight = 640;
            if (settings.WindowMaximized)
            {
                presenter.Maximize();
            }
        }

        ShellHost.Content = new AppShell(viewModel);
    }

    private RectInt32 GetRestoredBounds(AppSettings settings)
    {
        PointInt32 desiredPoint = settings.WindowX is int savedX && settings.WindowY is int savedY
            ? new PointInt32(savedX, savedY)
            : AppWindow.Position;
        DisplayArea area = settings.WindowX.HasValue && settings.WindowY.HasValue
            ? DisplayArea.GetFromPoint(desiredPoint, DisplayAreaFallback.Nearest)
            : DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        RectInt32 work = area.WorkArea;
        int width = Math.Min(settings.WindowWidth, work.Width);
        int height = Math.Min(settings.WindowHeight, work.Height);
        int x = settings.WindowX is int requestedX
            ? Math.Clamp(requestedX, work.X, work.X + work.Width - width)
            : work.X + ((work.Width - width) / 2);
        int y = settings.WindowY is int requestedY
            ? Math.Clamp(requestedY, work.Y, work.Y + work.Height - height)
            : work.Y + ((work.Height - height) / 2);
        return new RectInt32(x, y, width, height);
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
        await SaveWindowPlacementAsync();
        if (await requestClose())
        {
            AllowCloseAndClose();
        }
        else
        {
            AppWindow.Hide();
        }
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (sender.Presenter is OverlappedPresenter presenter)
        {
            isMaximized = presenter.State == OverlappedPresenterState.Maximized;
            if (presenter.State == OverlappedPresenterState.Restored)
            {
                restoredBounds = new RectInt32(sender.Position.X, sender.Position.Y, sender.Size.Width, sender.Size.Height);
            }
        }
    }

    private async ValueTask SaveWindowPlacementAsync()
    {
        RectInt32 bounds = restoredBounds.Width >= 960 && restoredBounds.Height >= 640
            ? restoredBounds
            : new RectInt32(AppWindow.Position.X, AppWindow.Position.Y, AppWindow.Size.Width, AppWindow.Size.Height);
        AppSettings current = await settingsStore.LoadAsync();
        await settingsStore.SaveAsync(current with
        {
            WindowX = bounds.X,
            WindowY = bounds.Y,
            WindowWidth = Math.Max(960, bounds.Width),
            WindowHeight = Math.Max(640, bounds.Height),
            WindowMaximized = isMaximized,
        });
    }
}
