using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using ThymeMe.Application.DataPortability;
using ThymeMe.Application.Lifecycle;
using ThymeMe.Application.Timing;
using ThymeMe.Infrastructure;
using ThymeMe.Platform.Windows;
using ThymeMe.Presentation.WinUI;
using ThymeMe.Presentation.WinUI.Shell;
using Windows.Storage;

namespace ThymeMe.App;

public partial class App : Microsoft.UI.Xaml.Application
{
    private static readonly Action<ILogger, Exception?> LogStartupFailure = LoggerMessage.Define(
        LogLevel.Critical,
        new EventId(1000, nameof(LogStartupFailure)),
        "Thyme-Me failed during startup.");

    private static readonly Action<ILogger, Exception?> LogUnhandledUiException = LoggerMessage.Define(
        LogLevel.Critical,
        new EventId(1001, nameof(LogUnhandledUiException)),
        "An unhandled UI exception terminated Thyme-Me.");

    private readonly IHost host;
    private static string? isolatedTestDataDirectory;
    private MainWindow? window;
    private ITrayService? trayService;

    public App()
    {
        InitializeComponent();

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddDebug();

        string applicationDataDirectory = GetApplicationDataDirectory();
        builder.Services.AddThymeMeInfrastructure(applicationDataDirectory);
        builder.Services.AddThymeMeWindowsPlatform();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddThymeMePresentation();

        host = builder.Build();
        UnhandledException += OnUnhandledException;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            AppInstance mainInstance = AppInstance.FindOrRegisterForKey("thymeme.main");
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
                host.Services.GetRequiredService<ThymeMe.Application.Settings.IAppSettingsStore>(),
                host.Services.GetRequiredService<TimeProvider>(),
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
            WriteIsolatedStartupFailure(exception);
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

    private static string GetApplicationDataDirectory()
    {
        string? testDirectory = Environment.GetEnvironmentVariable("THYMEME_TEST_DATA_DIRECTORY");
        if (string.Equals(Environment.GetEnvironmentVariable("THYMEME_TEST_MODE"), "1", StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(testDirectory))
        {
            string allowedRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "ThymeMe.Tests"));
            string requested = Path.GetFullPath(testDirectory);
            string allowedPrefix = allowedRoot.EndsWith(Path.DirectorySeparatorChar)
                ? allowedRoot
                : allowedRoot + Path.DirectorySeparatorChar;
            if (!requested.StartsWith(allowedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The isolated test profile must be below the ThymeMe.Tests temporary directory.");
            }

            Directory.CreateDirectory(requested);
            isolatedTestDataDirectory = requested;
            return requested;
        }

        try
        {
            return ApplicationData.Current.LocalFolder.Path;
        }
        catch (Exception exception) when ((uint)exception.HResult == 0x80073D54)
        {
            string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string directory = Path.Combine(localApplicationData, "thymeme");
            Directory.CreateDirectory(directory);
            return directory;
        }
    }

    private static void WriteIsolatedStartupFailure(Exception exception)
    {
        if (isolatedTestDataDirectory is null)
        {
            return;
        }

        try
        {
            File.WriteAllText(Path.Combine(isolatedTestDataDirectory, "startup-error.txt"), exception.ToString());
        }
        catch
        {
            // The original startup failure remains authoritative.
        }
    }

}
