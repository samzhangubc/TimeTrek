using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TimeTrek.Application.Settings;
using TimeTrek.Infrastructure.Persistence;

namespace TimeTrek.Infrastructure.Settings;

public sealed class SqliteAppSettingsStore(
    IDbContextFactory<TimeTrekDbContext> contextFactory,
    TimeProvider timeProvider) : IAppSettingsStore
{
    private const string SettingsKey = "application";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        AllowTrailingCommas = false,
        MaxDepth = 16,
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        WriteIndented = false,
    };

    public async ValueTask<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using TimeTrekDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        SettingRow? row = await context.Settings.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Key == SettingsKey, cancellationToken)
            .ConfigureAwait(false);
        if (row is null)
        {
            return new AppSettings();
        }

        try
        {
            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(row.JsonValue, SerializerOptions);
            if (settings is null || settings.SchemaVersion != AppSettings.CurrentSchemaVersion)
            {
                throw new SettingsStoreException("The stored settings use an unsupported schema.");
            }

            return AppSettingsValidator.Validate(settings);
        }
        catch (JsonException exception)
        {
            throw new SettingsStoreException("TimeTrek could not read stored settings safely.", exception);
        }
    }

    public async ValueTask SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        AppSettings validated = AppSettingsValidator.Validate(settings);
        string json = JsonSerializer.Serialize(validated, SerializerOptions);
        if (json.Length > JsonAppSettingsStore.MaximumSettingsBytes)
        {
            throw new SettingsStoreException("The settings payload exceeds the supported size.");
        }

        await using TimeTrekDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        SettingRow? row = await context.Settings.SingleOrDefaultAsync(item => item.Key == SettingsKey, cancellationToken).ConfigureAwait(false);
        if (row is null)
        {
            context.Settings.Add(new SettingRow
            {
                Key = SettingsKey,
                JsonValue = json,
                SchemaVersion = AppSettings.CurrentSchemaVersion,
                UpdatedUtcMilliseconds = timeProvider.GetUtcNow().ToUnixTimeMilliseconds(),
            });
        }
        else
        {
            row.JsonValue = json;
            row.SchemaVersion = AppSettings.CurrentSchemaVersion;
            row.UpdatedUtcMilliseconds = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
