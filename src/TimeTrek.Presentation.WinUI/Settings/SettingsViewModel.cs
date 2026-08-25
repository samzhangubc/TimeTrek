using System.Reflection;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using TimeTrek.Application.ActivityTracking;
using TimeTrek.Application.DataPortability;
using TimeTrek.Application.Lifecycle;
using TimeTrek.Application.Settings;
using TimeTrek.Application.Updates;

namespace TimeTrek.Presentation.WinUI.Settings;

public sealed partial class SettingsViewModel(
    IAppSettingsStore settingsStore,
    IDataPortabilityService dataPortability,
    IUserFileDialogService fileDialogs,
    IStartupRegistrationService startupRegistration,
    IUpdateService updateService,
    IActivityStore activityStore,
    IClipboardService clipboard) : ObservableObject
{
    private static readonly Assembly PresentationAssembly = typeof(SettingsViewModel).Assembly;
    private readonly string displayVersion = ResolveDisplayVersion();
    private readonly string provenanceId = ResolveProvenanceId();
    private string? pendingRestorePath;

    public string DisplayVersion => displayVersion;

    public string AboutSummary => $"TimeTrek {DisplayVersion} · local-first · source available";

    public string ProvenanceId => provenanceId;

    private static string ResolveDisplayVersion() =>
        PresentationAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            .Split('+', 2)[0]
        ?? PresentationAssembly.GetName().Version?.ToString(3)
        ?? "unknown";

    private static string ResolveProvenanceId() => PresentationAssembly.GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(item => string.Equals(item.Key, "TimeTrek.ProvenanceId", StringComparison.Ordinal))
        ?.Value ?? "unavailable";

    [ObservableProperty]
    public partial AppSettings Current { get; set; } = new();

    [ObservableProperty]
    public partial string SaveStatus { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public async ValueTask LoadAsync(CancellationToken cancellationToken = default) =>
        Current = await settingsStore.LoadAsync(cancellationToken);

    public async ValueTask SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        try
        {
            await settingsStore.SaveAsync(settings, cancellationToken);
            await startupRegistration.SetEnabledAsync(settings.StartWithWindows, cancellationToken);
            Current = settings;
            SaveStatus = "Saved";
        }
        catch (Exception exception)
        {
            SaveStatus = $"Not saved: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public ValueTask ExportJsonAsync(CancellationToken cancellationToken = default) =>
        ExportAsync(ExportFormat.Json, "TimeTrek export", ".json", cancellationToken);

    public ValueTask ExportCsvAsync(CancellationToken cancellationToken = default) =>
        ExportAsync(ExportFormat.CsvBundle, "TimeTrek CSV bundle", ".zip", cancellationToken);

    public async ValueTask CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        string? path = await fileDialogs.PickSavePathAsync(
            $"TimeTrek-backup-{DateTime.Now:yyyy-MM-dd}", "TimeTrek backup", ".zip", cancellationToken);
        if (path is null)
        {
            return;
        }

        await RunAsync(async () =>
        {
            await using FileStream stream = CreateWriteStream(path);
            await dataPortability.CreateBackupAsync(stream, cancellationToken);
            SaveStatus = "Backup created";
        });
    }

    public async ValueTask<RestorePreview?> PreviewRestoreAsync(CancellationToken cancellationToken = default)
    {
        string? path = await fileDialogs.PickOpenPathAsync("TimeTrek backup", [".zip"], cancellationToken);
        if (path is null)
        {
            return null;
        }

        pendingRestorePath = path;
        await using FileStream stream = CreateReadStream(path);
        return await dataPortability.PreviewRestoreAsync(stream, RestoreMode.Replace, cancellationToken);
    }

    public async ValueTask RestoreAsync(RestoreMode mode, CancellationToken cancellationToken = default)
    {
        string path = pendingRestorePath ?? throw new InvalidOperationException("Choose and validate a backup first.");
        await RunAsync(async () =>
        {
            await using FileStream stream = CreateReadStream(path);
            await dataPortability.RestoreAsync(stream, mode, cancellationToken);
            pendingRestorePath = null;
            SaveStatus = "Backup restored";
        });
    }

    public async ValueTask CheckForUpdatesAsync(CancellationToken cancellationToken = default) =>
        await RunAsync(async () =>
        {
            UpdateInfo? update = await updateService.CheckAsync(cancellationToken);
            SaveStatus = update is null ? "TimeTrek is up to date" : $"Version {update.Version} is available";
        });

    public async ValueTask PurgeActivityAsync(CancellationToken cancellationToken = default) =>
        await RunAsync(async () =>
        {
            await activityStore.PurgeAllAsync(cancellationToken);
            SaveStatus = "Application activity deleted";
        });

    public async ValueTask CopyDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        string diagnostics = string.Join(Environment.NewLine,
        [
            $"TimeTrek version: {DisplayVersion}",
            $"TimeTrek provenance: {ProvenanceId}",
            $"Windows: {RuntimeInformation.OSDescription}",
            $"Architecture: {RuntimeInformation.ProcessArchitecture}",
            $"Setup complete: {Current.BasicSetupCompleted}",
            $"Appearance: {Current.AppearanceMode}",
            $"Notifications: {Current.NotificationsEnabled}",
            $"Foreground tracking: {Current.ForegroundTrackingEnabled}",
        ]);
        await clipboard.SetTextAsync(diagnostics, cancellationToken);
        SaveStatus = "Diagnostics copied";
    }

    private async ValueTask ExportAsync(ExportFormat format, string displayName, string extension, CancellationToken cancellationToken)
    {
        string? path = await fileDialogs.PickSavePathAsync(
            $"TimeTrek-export-{DateTime.Now:yyyy-MM-dd}", displayName, extension, cancellationToken);
        if (path is null)
        {
            return;
        }

        await RunAsync(async () =>
        {
            await using FileStream stream = CreateWriteStream(path);
            await dataPortability.ExportAsync(stream, new ExportRequest(format, true, null), cancellationToken);
            SaveStatus = "Export created";
        });
    }

    private async ValueTask RunAsync(Func<Task> operation)
    {
        IsBusy = true;
        try
        {
            await operation();
        }
        catch (Exception exception)
        {
            SaveStatus = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static FileStream CreateWriteStream(string path) => new(path, new FileStreamOptions
    {
        Access = FileAccess.Write,
        Mode = FileMode.Create,
        Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
        Share = FileShare.None,
    });

    private static FileStream CreateReadStream(string path) => new(path, new FileStreamOptions
    {
        Access = FileAccess.Read,
        Mode = FileMode.Open,
        Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
        Share = FileShare.Read,
    });
}
