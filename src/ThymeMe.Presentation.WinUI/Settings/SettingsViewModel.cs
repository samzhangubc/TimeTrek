using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using ThymeMe.Application;
using ThymeMe.Application.ActivityTracking;
using ThymeMe.Application.Appearance;
using ThymeMe.Application.DataPortability;
using ThymeMe.Application.Lifecycle;
using ThymeMe.Application.Organization;
using ThymeMe.Application.Settings;
using ThymeMe.Application.Updates;
using ThymeMe.Domain.Appearance;
using ThymeMe.Domain.Organization;

namespace ThymeMe.Presentation.WinUI.Settings;

public sealed partial class SettingsViewModel(
    IAppSettingsStore settingsStore,
    IDataPortabilityService dataPortability,
    IUserFileDialogService fileDialogs,
    IStartupRegistrationService startupRegistration,
    IUpdateService updateService,
    IActivityStore activityStore,
    IClipboardService clipboard,
    AppearancePaletteService appearancePaletteService,
    OrganizationService organizationService) : ObservableObject, IDisposable
{
    private static readonly Assembly PresentationAssembly = typeof(SettingsViewModel).Assembly;
    private readonly string displayVersion = ResolveDisplayVersion();
    private readonly string provenanceId = ResolveProvenanceId();
    private string? pendingRestorePath;
    private readonly SemaphoreSlim saveGate = new(1, 1);

    public event Action<AppSettings>? SettingsApplied;

    public event Action? RerunSetupRequested;

    public string DisplayVersion => displayVersion;

    public string AboutSummary => $"{ProductIdentity.DisplayName} {DisplayVersion} · local-first · source available";

    public string ProvenanceId => provenanceId;

    private static string ResolveDisplayVersion() =>
        PresentationAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            .Split('+', 2)[0]
        ?? PresentationAssembly.GetName().Version?.ToString(3)
        ?? "unknown";

    private static string ResolveProvenanceId() => PresentationAssembly.GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(item => string.Equals(item.Key, "ThymeMe.ProvenanceId", StringComparison.Ordinal))
        ?.Value ?? "unavailable";

    [ObservableProperty]
    public partial AppSettings Current { get; set; } = new();

    [ObservableProperty]
    public partial string SaveStatus { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public ObservableCollection<PaletteListItem> Palettes { get; } = [];

    public async ValueTask LoadAsync(CancellationToken cancellationToken = default)
    {
        Current = await settingsStore.LoadAsync(cancellationToken);
        await ReloadPalettesAsync(cancellationToken);
        SettingsApplied?.Invoke(Current);
    }

    public async ValueTask<bool> SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        await saveGate.WaitAsync(cancellationToken);
        AppSettings previous = Current;
        bool startupChanged = settings.StartWithWindows != previous.StartWithWindows;
        try
        {
            IsBusy = true;
            SaveStatus = "Saving…";
            if (startupChanged)
            {
                await startupRegistration.SetEnabledAsync(settings.StartWithWindows, cancellationToken);
            }

            await settingsStore.SaveAsync(settings, cancellationToken);
            Current = settings;
            SaveStatus = "Saved";
            SettingsApplied?.Invoke(settings);
            return true;
        }
        catch (Exception exception)
        {
            if (startupChanged)
            {
                try
                {
                    await startupRegistration.SetEnabledAsync(previous.StartWithWindows, CancellationToken.None);
                }
                catch
                {
                    // The original failure remains the actionable error; the next load re-reads durable state.
                }
            }

            SaveStatus = $"Not saved: {exception.Message}";
            return false;
        }
        finally
        {
            IsBusy = false;
            saveGate.Release();
        }
    }

    public AppearancePaletteOption ResolvePalette(string? key)
    {
        PaletteListItem? item = Palettes.FirstOrDefault(value => string.Equals(value.Key, key, StringComparison.Ordinal));
        return item?.Option ?? AppearancePaletteService.BuiltIn[0];
    }

    public async ValueTask<string?> SaveCustomPaletteAsync(
        Guid? id,
        string name,
        PaletteVariant light,
        PaletteVariant dark,
        CancellationToken cancellationToken = default)
    {
        try
        {
            AppearancePaletteOption saved = id is Guid existing
                ? await appearancePaletteService.UpdateAsync(existing, name, light, dark, cancellationToken)
                : await appearancePaletteService.CreateAsync(name, light, dark, cancellationToken);
            await ReloadPalettesAsync(cancellationToken);
            SaveStatus = "Palette saved";
            return saved.Key;
        }
        catch (Exception exception)
        {
            SaveStatus = $"Palette not saved: {exception.Message}";
            return null;
        }
    }

    public async ValueTask<string?> DuplicatePaletteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            AppearancePaletteOption copy = await appearancePaletteService.DuplicateAsync(id, cancellationToken);
            await ReloadPalettesAsync(cancellationToken);
            SaveStatus = "Palette duplicated";
            return copy.Key;
        }
        catch (Exception exception)
        {
            SaveStatus = exception.Message;
            return null;
        }
    }

    public async ValueTask<bool> DeletePaletteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await appearancePaletteService.DeleteAsync(id, cancellationToken);
            await ReloadPalettesAsync(cancellationToken);
            SaveStatus = "Palette deleted";
            return true;
        }
        catch (Exception exception)
        {
            SaveStatus = exception.Message;
            return false;
        }
    }

    public async ValueTask ExportPaletteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        string? path = await fileDialogs.PickSavePathAsync("Thyme-Me-palette", "Thyme-Me palette", ".json", cancellationToken);
        if (path is null)
        {
            return;
        }

        await RunAsync(async () =>
        {
            await using FileStream stream = CreateWriteStream(path);
            await appearancePaletteService.ExportAsync(id, stream, cancellationToken);
            SaveStatus = "Palette exported";
        });
    }

    public async ValueTask<string?> ImportPaletteAsync(CancellationToken cancellationToken = default)
    {
        string? path = await fileDialogs.PickOpenPathAsync("Thyme-Me palette", [".json"], cancellationToken);
        if (path is null)
        {
            return null;
        }

        try
        {
            await using FileStream stream = CreateReadStream(path);
            AppearancePaletteOption imported = await appearancePaletteService.ImportAsync(stream, cancellationToken);
            await ReloadPalettesAsync(cancellationToken);
            SaveStatus = "Palette imported";
            return imported.Key;
        }
        catch (Exception exception)
        {
            SaveStatus = $"Palette not imported: {exception.Message}";
            return null;
        }
    }

    public static string PaletteSignature(PaletteVariant light, PaletteVariant dark) =>
        string.Join('|', light.Canvas, light.Surface, light.Accent, dark.Canvas, dark.Surface, dark.Accent).ToUpperInvariant();

    private async ValueTask ReloadPalettesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<AppearancePaletteOption> palettes = await appearancePaletteService.ListAsync(cancellationToken);
        Palettes.Clear();
        foreach (AppearancePaletteOption palette in palettes)
        {
            Palettes.Add(new PaletteListItem(palette));
        }
    }

    public ValueTask ExportJsonAsync(bool includeApplicationBreakdown, CancellationToken cancellationToken = default) =>
        ExportAsync(ExportFormat.Json, includeApplicationBreakdown, $"{ProductIdentity.DisplayName} export", ".json", cancellationToken);

    public ValueTask ExportCsvAsync(bool includeApplicationBreakdown, CancellationToken cancellationToken = default) =>
        ExportAsync(ExportFormat.CsvBundle, includeApplicationBreakdown, $"{ProductIdentity.DisplayName} CSV bundle", ".zip", cancellationToken);

    public void RequestRerunSetup() => RerunSetupRequested?.Invoke();

    public async ValueTask CreateSampleAsync(bool math, CancellationToken cancellationToken = default)
    {
        string name = math ? "MATH 100" : "Work";
        if ((await organizationService.ListStreamsAsync(false, cancellationToken)).Any(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            SaveStatus = $"{name} already exists";
            return;
        }

        Application.Common.OperationResult<StreamDefinition> created = await organizationService.CreateStreamAsync(name, cancellationToken: cancellationToken);
        if (created.Value is not StreamDefinition stream)
        {
            SaveStatus = created.Error?.Message ?? $"Could not create {name}";
            return;
        }

        if (math)
        {
            _ = await organizationService.CreateCategoryAsync("Lecture", stream.Id, false, null, null, cancellationToken);
            _ = await organizationService.CreateCategoryAsync("Homework", stream.Id, false, null, null, cancellationToken);
        }
        else
        {
            _ = await organizationService.CreateCategoryAsync("Focused work", stream.Id, false, null, null, cancellationToken);
            _ = await organizationService.CreateProjectAsync("Example project", stream.Id, cancellationToken);
        }

        SaveStatus = $"{name} sample created";
    }

    public async ValueTask CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        string? path = await fileDialogs.PickSavePathAsync(
            $"{ProductIdentity.DisplayName}-backup-{DateTime.Now:yyyy-MM-dd}", $"{ProductIdentity.DisplayName} backup", ".zip", cancellationToken);
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
        string? path = await fileDialogs.PickOpenPathAsync($"{ProductIdentity.DisplayName} backup", [".zip"], cancellationToken);
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
            await LoadAsync(cancellationToken);
            SaveStatus = "Backup restored";
        });
    }

    public async ValueTask CheckForUpdatesAsync(CancellationToken cancellationToken = default) =>
        await RunAsync(async () =>
        {
            UpdateInfo? update = await updateService.CheckAsync(cancellationToken);
            SaveStatus = update is null ? $"{ProductIdentity.DisplayName} is up to date" : $"Version {update.Version} is available";
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
            $"{ProductIdentity.DisplayName} version: {DisplayVersion}",
            $"{ProductIdentity.DisplayName} provenance: {ProvenanceId}",
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

    private async ValueTask ExportAsync(ExportFormat format, bool includeApplicationBreakdown, string displayName, string extension, CancellationToken cancellationToken)
    {
        string? path = await fileDialogs.PickSavePathAsync(
            $"{ProductIdentity.DisplayName}-export-{DateTime.Now:yyyy-MM-dd}", displayName, extension, cancellationToken);
        if (path is null)
        {
            return;
        }

        await RunAsync(async () =>
        {
            await using FileStream stream = CreateWriteStream(path);
            await dataPortability.ExportAsync(stream, new ExportRequest(format, includeApplicationBreakdown, null), cancellationToken);
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

    public void Dispose() => saveGate.Dispose();
}

public sealed record PaletteListItem(AppearancePaletteOption Option)
{
    public string Key => Option.Key;

    public Guid Id => Option.Palette.Id;

    public string Name => Option.Palette.Name;

    public bool IsBuiltIn => Option.Palette.IsBuiltIn;

    public string LightSwatch => $"{Option.Palette.Light.Canvas} · {Option.Palette.Light.Surface} · {Option.Palette.Light.Accent}";

    public string DarkSwatch => $"{Option.Palette.Dark.Canvas} · {Option.Palette.Dark.Surface} · {Option.Palette.Dark.Accent}";
}
