using System.Text.Json;
using TimeTrek.Application.Settings;

namespace TimeTrek.Infrastructure.Settings;

public sealed class JsonAppSettingsStore : IAppSettingsStore, IDisposable
{
    internal const int MaximumSettingsBytes = 64 * 1024;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        AllowTrailingCommas = false,
        MaxDepth = 16,
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        WriteIndented = true,
    };

    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly string settingsPath;
    private bool disposed;

    public JsonAppSettingsStore(string applicationDataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDataDirectory);
        settingsPath = Path.Combine(Path.GetFullPath(applicationDataDirectory), "settings.json");
    }

    public async ValueTask<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!File.Exists(settingsPath))
            {
                return new AppSettings();
            }

            FileInfo file = new(settingsPath);
            if (file.Length is <= 0 or > MaximumSettingsBytes)
            {
                throw new SettingsStoreException("The settings file has an invalid size.");
            }

            await using FileStream stream = new(
                settingsPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            AppSettings? settings = await JsonSerializer.DeserializeAsync<AppSettings>(
                stream,
                SerializerOptions,
                cancellationToken).ConfigureAwait(false);

            if (settings is null || settings.SchemaVersion != AppSettings.CurrentSchemaVersion)
            {
                throw new SettingsStoreException("The settings file uses an unsupported schema.");
            }

            return AppSettingsValidator.Validate(settings);
        }
        catch (SettingsStoreException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new SettingsStoreException("TimeTrek could not read the settings file safely.", exception);
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask SaveAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(settings);
        AppSettings validated = AppSettingsValidator.Validate(settings);
        byte[] content = JsonSerializer.SerializeToUtf8Bytes(validated, SerializerOptions);

        if (content.Length > MaximumSettingsBytes)
        {
            throw new SettingsStoreException("The settings payload exceeds the supported size.");
        }

        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        string? temporaryPath = null;

        try
        {
            string directory = Path.GetDirectoryName(settingsPath)!;
            Directory.CreateDirectory(directory);
            temporaryPath = Path.Combine(directory, $"settings.{Guid.CreateVersion7():N}.tmp");

            await using (FileStream stream = new(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(content, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, settingsPath, overwrite: true);
            temporaryPath = null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new SettingsStoreException("TimeTrek could not save settings durably.", exception);
        }
        finally
        {
            if (temporaryPath is not null)
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            gate.Release();
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        gate.Dispose();
        disposed = true;
    }

}
