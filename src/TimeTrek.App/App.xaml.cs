using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using TimeTrek.Application.DataPortability;
using TimeTrek.Application.Lifecycle;
using TimeTrek.Application.Timing;
using TimeTrek.Infrastructure;
using TimeTrek.Platform.Windows;
using TimeTrek.Presentation.WinUI;
using TimeTrek.Presentation.WinUI.Shell;
using Windows.Storage;

namespace TimeTrek.App;

public partial class App : Microsoft.UI.Xaml.Application
{
    private static readonly Action<ILogger, Exception?> LogStartupFailure = LoggerMessage.Define(
        LogLevel.Critical,
        new EventId(1000, nameof(LogStartupFailure)),
        "TimeTrek failed during startup.");

    private static readonly Action<ILogger, Exception?> LogUnhandledUiException = LoggerMessage.Define(
        LogLevel.Critical,
        new EventId(1001, nameof(LogUnhandledUiException)),
        "An unhandled UI exception terminated TimeTrek.");

    private readonly IHost host;
    private MainWindow? window;
    private ITrayService? trayService;

    public App()
    {
        InitializeComponent();

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddDebug();

        string applicationDataDirectory = ApplicationData.Current.LocalFolder.Path;
        builder.Services.AddTimeTrekInfrastructure(applicationDataDirectory);
        builder.Services.AddTimeTrekWindowsPlatform();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddTimeTrekPresentation();

        host = builder.Build();
        UnhandledException += OnUnhandledException;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            AppInstance mainInstance = AppInstance.FindOrRegisterForKey("TimeTrek.Main");
            if (!mainInstance.IsCurrent)
            {
                await mainInstance.RedirectActivationToAsync(AppInstance.GetCurrent().GetActivatedEventArgs());
                Exit();
                return;
            }

            await host.StartAsync();
            AppShellViewModel viewModel = host.Services.GetRequiredService<AppShellViewModel>();
            await viewModel.InitializeAsync();

            trayService = host.Services.GetRequiredService<ITrayService>();
            window = new MainWindow(
                viewModel,
                host.Services.GetRequiredService<TimeTrek.Application.Settings.IAppSettingsStore>(),
                OnCloseRequestedAsync);
            window.Closed += OnWindowClosed;
            window.Activate();
            nint handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
            await trayService.InitializeAsync(handle, Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));
            await host.Services.GetRequiredService<IInterruptionSource>().InitializeAsync(handle);
            await host.Services.GetRequiredService<IUserFileDialogService>().InitializeAsync(handle);
            trayService.RestoreRequested += OnTrayRestoreRequested;
        }
        catch (Exception exception)
        {
            ILogger<App> logger = host.Services.GetRequiredService<ILogger<App>>();
            LogStartupFailure(logger, exception);
            Exit();
        }
    }

    private async void OnWindowClosed(object sender, WindowEventArgs args)
    {
        if (trayService is not null)
        {
            trayService.RestoreRequested -= OnTrayRestoreRequested;
            await trayService.HideAsync();
        }

        if (window is not null)
        {
            window.Closed -= OnWindowClosed;
        }

        await host.StopAsync(TimeSpan.FromSeconds(5));
        host.Dispose();
    }

    private async ValueTask<bool> OnCloseRequestedAsync()
    {
        TimingCoordinator timing = host.Services.GetRequiredService<TimingCoordinator>();
        ITimingStore store = host.Services.GetRequiredService<ITimingStore>();
        bool keepRunning = await timing.GetActiveAsync() is not null ||
            (await store.ListPendingCompletionsAsync(1)).Count > 0;
        if (keepRunning && trayService is not null)
        {
            await trayService.ShowAsync();
        }

        return !keepRunning;
    }

    private void OnTrayRestoreRequested(object? sender, EventArgs e) =>
        window?.DispatcherQueue.TryEnqueue(() => window.Restore());

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        ILogger<App> logger = host.Services.GetRequiredService<ILogger<App>>();
        LogUnhandledUiException(logger, e.Exception);
    }
}
