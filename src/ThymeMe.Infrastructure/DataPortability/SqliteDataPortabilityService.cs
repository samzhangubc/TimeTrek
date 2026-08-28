using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ThymeMe.Application;
using ThymeMe.Application.DataPortability;
using ThymeMe.Application.Settings;
using ThymeMe.Domain.Common;
using ThymeMe.Domain.DataPortability;
using ThymeMe.Infrastructure.Persistence;

namespace ThymeMe.Infrastructure.DataPortability;

public sealed class SqliteDataPortabilityService(
    IDbContextFactory<ThymeMeDbContext> contextFactory,
    IAppSettingsStore settingsStore,
    string applicationDataDirectory) : IDataPortabilityService
{
    private const string CurrentSchemaVersion = "1.0";
    private const string DataEntryName = "data.json";
    private const string ManifestEntryName = "manifest.json";
    private const long MaximumInMemoryRestoreBytes = 512L * 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        MaxDepth = DefensiveLimits.MaximumJsonDepth,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
    };

    private readonly string applicationDataDirectory = Path.GetFullPath(applicationDataDirectory);

    public async ValueTask ExportAsync(
        Stream destination,
        ExportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        PortableData snapshot = await ReadSnapshotAsync(request.IncludeApplicationBreakdown, cancellationToken).ConfigureAwait(false);
        if (request.Format == ExportFormat.Json)
        {
            await JsonSerializer.SerializeAsync(destination, snapshot, JsonOptions, cancellationToken).ConfigureAwait(false);
            return;
        }

        using ZipArchive archive = new(destination, ZipArchiveMode.Create, leaveOpen: true);
        await WriteCsvAsync(archive, snapshot, request.IncludeApplicationBreakdown, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask CreateBackupAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        if (await context.TimingRoots.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Backup is deferred until the active Session stops.");
        }

        PortableData snapshot = await ReadSnapshotAsync(includeApplications: true, cancellationToken).ConfigureAwait(false);
        string temporaryDirectory = Path.Combine(applicationDataDirectory, "Temp", Guid.CreateVersion7().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);
        string dataPath = Path.Combine(temporaryDirectory, DataEntryName);

        try
        {
            await using (FileStream file = new(dataPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65_536, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await JsonSerializer.SerializeAsync(file, snapshot, JsonOptions, cancellationToken).ConfigureAwait(false);
                await file.FlushAsync(cancellationToken).ConfigureAwait(false);
                file.Flush(flushToDisk: true);
            }

            FileInfo dataFile = new(dataPath);
            string checksum = await ComputeSha256Async(dataPath, cancellationToken).ConfigureAwait(false);
            BackupManifest manifest = new(ProductIdentity.DisplayName, CurrentSchemaVersion, snapshot.CreatedUtc, DataEntryName, dataFile.Length, checksum);
            using ZipArchive archive = new(destination, ZipArchiveMode.Create, leaveOpen: true);
            ZipArchiveEntry dataEntry = archive.CreateEntry(DataEntryName, CompressionLevel.Optimal);
            await using (Stream entryStream = dataEntry.Open())
            await using (FileStream source = new(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read, 65_536, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await source.CopyToAsync(entryStream, 65_536, cancellationToken).ConfigureAwait(false);
            }

            ZipArchiveEntry manifestEntry = archive.CreateEntry(ManifestEntryName, CompressionLevel.Optimal);
            await using Stream manifestStream = manifestEntry.Open();
            await JsonSerializer.SerializeAsync(manifestStream, manifest, JsonOptions, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }
        }
    }

    public async ValueTask<RestorePreview> PreviewRestoreAsync(
        Stream source,
        RestoreMode mode,
        CancellationToken cancellationToken = default)
    {
        PortableData data = await ReadAndValidateBackupAsync(source, cancellationToken).ConfigureAwait(false);
        int conflicts = 0;
        if (mode == RestoreMode.Merge)
        {
            await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            HashSet<Guid> incoming = data.Streams.Select(item => item.Id)
                .Concat(data.Categories.Select(item => item.Id))
                .Concat(data.Projects.Select(item => item.Id))
                .Concat(data.Sessions.Select(item => item.Id))
                .ToHashSet();
            Guid[] existing = (await context.Streams.Select(item => item.Id).ToListAsync(cancellationToken).ConfigureAwait(false))
                .Concat(await context.Categories.Select(item => item.Id).ToListAsync(cancellationToken).ConfigureAwait(false))
                .Concat(await context.Projects.Select(item => item.Id).ToListAsync(cancellationToken).ConfigureAwait(false))
                .Concat(await context.Sessions.Select(item => item.Id).ToListAsync(cancellationToken).ConfigureAwait(false))
                .ToArray();
            conflicts = existing.Count(incoming.Contains);
        }

        return new(
            data.Streams.Count,
            data.Categories.Count,
            data.Projects.Count,
            data.Sessions.Count,
            conflicts,
            data.SchemaVersion);
    }

    public async ValueTask RestoreAsync(
        Stream source,
        RestoreMode mode,
        CancellationToken cancellationToken = default)
    {
        PortableData data = await ReadAndValidateBackupAsync(source, cancellationToken).ConfigureAwait(false);
        string rollbackDirectory = Path.Combine(applicationDataDirectory, "Rollback");
        Directory.CreateDirectory(rollbackDirectory);
        string rollbackPath = Path.Combine(rollbackDirectory, $"before-restore-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.thymeme-backup");
        await using (FileStream rollback = new(rollbackPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65_536, FileOptions.Asynchronous))
        {
            await CreateBackupAsync(rollback, cancellationToken).ConfigureAwait(false);
        }

        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        if (await context.TimingRoots.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Restore is deferred until the active Session stops.");
        }

        if (mode == RestoreMode.Replace)
        {
            await DeleteLiveDataAsync(context, cancellationToken).ConfigureAwait(false);
        }

        RestoreMaps maps = await BuildRestoreMapsAsync(context, data, mode, cancellationToken).ConfigureAwait(false);
        AddData(context, data, maps, mode);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        await settingsStore.SaveAsync(data.Settings, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<PortableData> ReadSnapshotAsync(bool includeApplications, CancellationToken cancellationToken)
    {
        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        AppSettings settings = await settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        List<StreamRow> streams = await context.Streams.AsNoTracking().Where(item => !item.IsDeleted).ToListAsync(cancellationToken).ConfigureAwait(false);
        List<CategoryRow> categories = await context.Categories.AsNoTracking().Where(item => !item.IsDeleted).ToListAsync(cancellationToken).ConfigureAwait(false);
        List<ProjectRow> projects = await context.Projects.AsNoTracking().Where(item => !item.IsDeleted).ToListAsync(cancellationToken).ConfigureAwait(false);
        List<SessionRow> sessions = await context.Sessions.AsNoTracking().Include(item => item.Categories)
            .Where(item => !item.IsDeleted && !item.IsDraft).ToListAsync(cancellationToken).ConfigureAwait(false);
        List<AdjustmentRow> adjustments = await context.Adjustments.AsNoTracking().Include(item => item.Categories)
            .Where(item => !item.IsDeleted).ToListAsync(cancellationToken).ConfigureAwait(false);
        List<ForegroundApplicationRow> applications = includeApplications
            ? await context.ForegroundApplications.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false)
            : [];
        List<PaletteRow> palettes = await context.Palettes.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        return new(
            CurrentSchemaVersion,
            DateTimeOffset.UtcNow,
            streams.Select(item => new PortableStream(item.Id, item.Name, item.Color, item.SortOrder, item.BudgetMilliseconds, item.BudgetResetPeriod, item.IsArchived, item.ArchivedUtcMilliseconds, item.CreatedUtcMilliseconds, item.UpdatedUtcMilliseconds)).ToList(),
            categories.Select(item => new PortableCategory(item.Id, item.Name, item.StreamId, item.DefaultBillable, item.HourlyRateMinorUnits, item.CurrencyCode, item.IsArchived, item.ArchivedUtcMilliseconds, item.CreatedUtcMilliseconds, item.UpdatedUtcMilliseconds)).ToList(),
            projects.Select(item => new PortableProject(item.Id, item.Name, item.StreamId, item.BudgetMilliseconds, item.BudgetResetPeriod, item.IsArchived, item.ArchivedUtcMilliseconds, item.CreatedUtcMilliseconds, item.UpdatedUtcMilliseconds)).ToList(),
            sessions.Select(item => new PortableSession(item.Id, item.Origin, item.TimingMode, item.StreamId, item.ProjectId, item.Categories.Select(join => join.CategoryId).ToList(), item.StartUtcMilliseconds, item.EndUtcMilliseconds, item.TimeZoneId, item.StartUtcOffsetMinutes, item.EndUtcOffsetMinutes, item.RawDurationMilliseconds, item.EffectiveDurationMilliseconds, item.RoundingIncrementMinutes, item.RoundingRule, item.RequestedDurationMilliseconds, item.Description, item.CompletionPending, item.IsBillable, item.HourlyRateMinorUnits, item.CurrencyCode, item.EstimatedEarningMinorUnits, item.CreatedUtcMilliseconds, item.UpdatedUtcMilliseconds)).ToList(),
            adjustments.Select(item => new PortableAdjustment(item.Id, item.StreamId, item.ProjectId, item.Categories.Select(join => join.CategoryId).ToList(), item.LocalDateUnixDays, item.TimeZoneId, item.RawDurationMilliseconds, item.EffectiveDurationMilliseconds, item.RoundingIncrementMinutes, item.RoundingRule, item.IsBillable, item.HourlyRateMinorUnits, item.CurrencyCode, item.EstimatedEarningMinorUnits, item.Description, item.CreatedUtcMilliseconds)).ToList(),
            applications.Select(item => new PortableApplication(item.Id, item.SessionId, item.DisplayName, item.ExecutableFileName, item.DurationMilliseconds)).ToList(),
            palettes.Select(item => new PortablePalette(item.Id, item.Name, item.FormatVersion, item.IsBuiltIn, item.LightCanvas, item.LightSurface, item.LightAccent, item.DarkCanvas, item.DarkSurface, item.DarkAccent)).ToList(),
            settings);
    }

    private static async Task WriteCsvAsync(
        ZipArchive archive,
        PortableData data,
        bool includeApplications,
        CancellationToken cancellationToken)
    {
        ZipArchiveEntry sessionsEntry = archive.CreateEntry("sessions.csv", CompressionLevel.Optimal);
        await using (Stream stream = sessionsEntry.Open())
        await using (StreamWriter writer = new(stream, new UTF8Encoding(false), 65_536, leaveOpen: false))
        {
            await writer.WriteLineAsync("schema_version,session_id,start_utc,end_utc,timezone,raw_seconds,rounded_seconds,rounding_increment,origin,timing_mode,stream_id,category_ids,project_id,description,billable,wage_minor_units,currency").ConfigureAwait(false);
            foreach (PortableSession session in data.Sessions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string[] fields =
                [
                    data.SchemaVersion,
                    session.Id.ToString("D"),
                    DateTimeOffset.FromUnixTimeMilliseconds(session.StartUtcMilliseconds).ToString("O", CultureInfo.InvariantCulture),
                    DateTimeOffset.FromUnixTimeMilliseconds(session.EndUtcMilliseconds).ToString("O", CultureInfo.InvariantCulture),
                    session.TimeZoneId,
                    (session.RawDurationMilliseconds / 1000m).ToString(CultureInfo.InvariantCulture),
                    (session.EffectiveDurationMilliseconds / 1000m).ToString(CultureInfo.InvariantCulture),
                    session.RoundingIncrementMinutes?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    session.Origin.ToString(CultureInfo.InvariantCulture),
                    session.TimingMode.ToString(CultureInfo.InvariantCulture),
                    session.StreamId?.ToString("D") ?? string.Empty,
                    JsonSerializer.Serialize(session.CategoryIds),
                    session.ProjectId?.ToString("D") ?? string.Empty,
                    NeutralizeCsvText(session.Description ?? string.Empty),
                    session.IsBillable ? "true" : "false",
                    session.HourlyRateMinorUnits?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    session.CurrencyCode ?? string.Empty,
                ];
                await writer.WriteLineAsync(string.Join(',', fields.Select(QuoteCsv))).ConfigureAwait(false);
            }
        }

        if (includeApplications)
        {
            ZipArchiveEntry appsEntry = archive.CreateEntry("applications.csv", CompressionLevel.Optimal);
            await using Stream stream = appsEntry.Open();
            await using StreamWriter writer = new(stream, new UTF8Encoding(false), 65_536, leaveOpen: false);
            await writer.WriteLineAsync("session_id,application_name,executable_file,duration_seconds").ConfigureAwait(false);
            foreach (PortableApplication application in data.Applications)
            {
                string[] fields =
                [
                    application.SessionId.ToString("D"),
                    NeutralizeCsvText(application.DisplayName),
                    NeutralizeCsvText(application.ExecutableFileName),
                    (application.DurationMilliseconds / 1000m).ToString(CultureInfo.InvariantCulture),
                ];
                await writer.WriteLineAsync(string.Join(',', fields.Select(QuoteCsv))).ConfigureAwait(false);
            }
        }

        ZipArchiveEntry dictionaryEntry = archive.CreateEntry("README.md", CompressionLevel.Optimal);
        await using Stream dictionaryStream = dictionaryEntry.Open();
        await using StreamWriter dictionaryWriter = new(dictionaryStream, new UTF8Encoding(false));
        await dictionaryWriter.WriteAsync($"# {ProductIdentity.DisplayName} export\n\nSchema 1.0. Durations are seconds; timestamps are ISO 8601 UTC. Category IDs are JSON arrays. Text beginning with =, +, -, @, tab, or carriage return is reversibly neutralized with one leading apostrophe for spreadsheet safety; remove that apostrophe only when the following character is one of those triggers. JSON and backups retain original text.\n").ConfigureAwait(false);
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65_536, FileOptions.Asynchronous | FileOptions.SequentialScan);
        byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash);
    }

    private static string NeutralizeCsvText(string value) => value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r'
        ? $"'{value}"
        : value;

    private static string QuoteCsv(string value) => $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    private static async Task<PortableData> ReadAndValidateBackupAsync(Stream source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        using ZipArchive archive = new(source, ZipArchiveMode.Read, leaveOpen: true);
        if (archive.Entries.Count is < 2 or > DefensiveLimits.MaximumArchiveEntries)
        {
            throw new InvalidDataException("The backup contains an invalid number of entries.");
        }

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (entry.FullName != DataEntryName && entry.FullName != ManifestEntryName)
            {
                throw new InvalidDataException("The backup contains an unexpected or unsafe entry path.");
            }

            if (entry.Length < 0 || entry.Length > MaximumInMemoryRestoreBytes ||
                entry.CompressedLength > 0 && entry.Length / entry.CompressedLength > DefensiveLimits.MaximumCompressionRatio)
            {
                throw new InvalidDataException("The backup exceeds safe expansion limits.");
            }
        }

        ZipArchiveEntry manifestEntry = archive.GetEntry(ManifestEntryName) ?? throw new InvalidDataException("The backup manifest is missing.");
        await using Stream manifestStream = manifestEntry.Open();
        BackupManifest manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(manifestStream, JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("The backup manifest is invalid.");
        if (!ProductIdentity.IsSupportedBackupProduct(manifest.Product) || manifest.SchemaVersion != CurrentSchemaVersion || manifest.DataEntry != DataEntryName)
        {
            throw new InvalidDataException("The backup schema is unsupported.");
        }

        ZipArchiveEntry dataEntry = archive.GetEntry(DataEntryName) ?? throw new InvalidDataException("The backup data is missing.");
        if (dataEntry.Length != manifest.DataBytes)
        {
            throw new InvalidDataException("The backup data length does not match its manifest.");
        }

        await using Stream dataStream = dataEntry.Open();
        using MemoryStream copy = new(checked((int)dataEntry.Length));
        await dataStream.CopyToAsync(copy, 65_536, cancellationToken).ConfigureAwait(false);
        string checksum = Convert.ToHexString(SHA256.HashData(copy.GetBuffer().AsSpan(0, checked((int)copy.Length))));
        if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(checksum), Convert.FromHexString(manifest.Sha256)))
        {
            throw new InvalidDataException("The backup checksum is invalid.");
        }

        copy.Position = 0;
        PortableData data = await JsonSerializer.DeserializeAsync<PortableData>(copy, JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("The backup data is invalid.");
        ValidateData(data);
        return data;
    }

    private static void ValidateData(PortableData data)
    {
        if (data.SchemaVersion != CurrentSchemaVersion ||
            data.Streams.Count + data.Categories.Count + data.Projects.Count > DefensiveLimits.MaximumOrganizationObjects ||
            data.Sessions.Count + data.Adjustments.Count > DefensiveLimits.MaximumSessionRecords ||
            data.Applications.Count > DefensiveLimits.MaximumActivityRecords)
        {
            throw new InvalidDataException("The backup record counts or schema are unsupported.");
        }

        HashSet<Guid> streamIds = data.Streams.Select(item => item.Id).ToHashSet();
        HashSet<Guid> categoryIds = data.Categories.Select(item => item.Id).ToHashSet();
        HashSet<Guid> projectIds = data.Projects.Select(item => item.Id).ToHashSet();
        HashSet<Guid> sessionIds = data.Sessions.Select(item => item.Id).ToHashSet();
        if (streamIds.Count != data.Streams.Count || categoryIds.Count != data.Categories.Count ||
            projectIds.Count != data.Projects.Count || sessionIds.Count != data.Sessions.Count)
        {
            throw new InvalidDataException("The backup contains duplicate stable identifiers.");
        }

        if (data.Categories.Any(item => item.StreamId.HasValue && !streamIds.Contains(item.StreamId.Value)) ||
            data.Projects.Any(item => item.StreamId.HasValue && !streamIds.Contains(item.StreamId.Value)) ||
            data.Sessions.Any(item => item.EndUtcMilliseconds < item.StartUtcMilliseconds ||
                item.StreamId.HasValue && !streamIds.Contains(item.StreamId.Value) ||
                item.ProjectId.HasValue && !projectIds.Contains(item.ProjectId.Value) ||
                item.CategoryIds.Any(id => !categoryIds.Contains(id))) ||
            data.Applications.Any(item => !sessionIds.Contains(item.SessionId)))
        {
            throw new InvalidDataException("The backup contains invalid relationships or time ranges.");
        }

        foreach (string name in data.Streams.Select(item => item.Name)
                     .Concat(data.Categories.Select(item => item.Name))
                     .Concat(data.Projects.Select(item => item.Name)))
        {
            _ = DomainText.RequiredName(name, "name");
        }
    }

    private static async Task DeleteLiveDataAsync(ThymeMeDbContext context, CancellationToken cancellationToken)
    {
        await context.ForegroundApplications.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.TimingSegments.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.SessionCategories.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.AdjustmentCategories.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.Sessions.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.Adjustments.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.Categories.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.Projects.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.Streams.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.Palettes.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<RestoreMaps> BuildRestoreMapsAsync(
        ThymeMeDbContext context,
        PortableData data,
        RestoreMode mode,
        CancellationToken cancellationToken)
    {
        if (mode == RestoreMode.Replace)
        {
            return RestoreMaps.Identity(data);
        }

        HashSet<Guid> existing = (await context.Streams.Select(item => item.Id).ToListAsync(cancellationToken).ConfigureAwait(false))
            .Concat(await context.Categories.Select(item => item.Id).ToListAsync(cancellationToken).ConfigureAwait(false))
            .Concat(await context.Projects.Select(item => item.Id).ToListAsync(cancellationToken).ConfigureAwait(false))
            .Concat(await context.Sessions.Select(item => item.Id).ToListAsync(cancellationToken).ConfigureAwait(false))
            .Concat(await context.Adjustments.Select(item => item.Id).ToListAsync(cancellationToken).ConfigureAwait(false))
            .ToHashSet();
        return RestoreMaps.Remap(data, existing);
    }

    private static void AddData(ThymeMeDbContext context, PortableData data, RestoreMaps maps, RestoreMode mode)
    {
        context.Streams.AddRange(data.Streams.Select(item => new StreamRow
        {
            Id = maps.Streams[item.Id],
            Name = item.Name,
            Color = item.Color,
            SortOrder = item.SortOrder,
            BudgetMilliseconds = item.BudgetMilliseconds,
            BudgetResetPeriod = item.BudgetResetPeriod,
            IsArchived = item.IsArchived,
            ArchivedUtcMilliseconds = item.ArchivedUtcMilliseconds,
            CreatedUtcMilliseconds = item.CreatedUtcMilliseconds,
            UpdatedUtcMilliseconds = item.UpdatedUtcMilliseconds,
        }));
        context.Categories.AddRange(data.Categories.Select(item => new CategoryRow
        {
            Id = maps.Categories[item.Id],
            Name = item.Name,
            StreamId = item.StreamId is Guid id ? maps.Streams[id] : null,
            DefaultBillable = item.DefaultBillable,
            HourlyRateMinorUnits = item.HourlyRateMinorUnits,
            CurrencyCode = item.CurrencyCode,
            IsArchived = item.IsArchived,
            ArchivedUtcMilliseconds = item.ArchivedUtcMilliseconds,
            CreatedUtcMilliseconds = item.CreatedUtcMilliseconds,
            UpdatedUtcMilliseconds = item.UpdatedUtcMilliseconds,
        }));
        context.Projects.AddRange(data.Projects.Select(item => new ProjectRow
        {
            Id = maps.Projects[item.Id],
            Name = item.Name,
            StreamId = item.StreamId is Guid id ? maps.Streams[id] : null,
            BudgetMilliseconds = item.BudgetMilliseconds,
            BudgetResetPeriod = item.BudgetResetPeriod,
            IsArchived = item.IsArchived,
            ArchivedUtcMilliseconds = item.ArchivedUtcMilliseconds,
            CreatedUtcMilliseconds = item.CreatedUtcMilliseconds,
            UpdatedUtcMilliseconds = item.UpdatedUtcMilliseconds,
        }));
        context.Sessions.AddRange(data.Sessions.Select(item => new SessionRow
        {
            Id = maps.Sessions[item.Id],
            Origin = item.Origin,
            TimingMode = item.TimingMode,
            IsDraft = false,
            StreamId = item.StreamId is Guid streamId ? maps.Streams[streamId] : null,
            ProjectId = item.ProjectId is Guid projectId ? maps.Projects[projectId] : null,
            StartUtcMilliseconds = item.StartUtcMilliseconds,
            EndUtcMilliseconds = item.EndUtcMilliseconds,
            TimeZoneId = item.TimeZoneId,
            StartUtcOffsetMinutes = item.StartUtcOffsetMinutes,
            EndUtcOffsetMinutes = item.EndUtcOffsetMinutes,
            RawDurationMilliseconds = item.RawDurationMilliseconds,
            EffectiveDurationMilliseconds = item.EffectiveDurationMilliseconds,
            RoundingIncrementMinutes = item.RoundingIncrementMinutes,
            RoundingRule = item.RoundingRule,
            RequestedDurationMilliseconds = item.RequestedDurationMilliseconds,
            Description = item.Description,
            CompletionPending = item.CompletionPending,
            IsBillable = item.IsBillable,
            HourlyRateMinorUnits = item.HourlyRateMinorUnits,
            CurrencyCode = item.CurrencyCode,
            EstimatedEarningMinorUnits = item.EstimatedEarningMinorUnits,
            CreatedUtcMilliseconds = item.CreatedUtcMilliseconds,
            UpdatedUtcMilliseconds = item.UpdatedUtcMilliseconds,
            Categories = item.CategoryIds.Select(categoryId => new SessionCategoryRow
            {
                SessionId = maps.Sessions[item.Id],
                CategoryId = maps.Categories[categoryId],
            }).ToList(),
        }));
        context.Adjustments.AddRange(data.Adjustments.Select(item => new AdjustmentRow
        {
            Id = maps.Adjustments[item.Id],
            StreamId = item.StreamId is Guid streamId ? maps.Streams[streamId] : null,
            ProjectId = item.ProjectId is Guid projectId ? maps.Projects[projectId] : null,
            LocalDateUnixDays = item.LocalDateUnixDays,
            TimeZoneId = item.TimeZoneId,
            RawDurationMilliseconds = item.RawDurationMilliseconds,
            EffectiveDurationMilliseconds = item.EffectiveDurationMilliseconds,
            RoundingIncrementMinutes = item.RoundingIncrementMinutes,
            RoundingRule = item.RoundingRule,
            IsBillable = item.IsBillable,
            HourlyRateMinorUnits = item.HourlyRateMinorUnits,
            CurrencyCode = item.CurrencyCode,
            EstimatedEarningMinorUnits = item.EstimatedEarningMinorUnits,
            Description = item.Description,
            CreatedUtcMilliseconds = item.CreatedUtcMilliseconds,
            Categories = item.CategoryIds.Select(categoryId => new AdjustmentCategoryRow
            {
                AdjustmentId = maps.Adjustments[item.Id],
                CategoryId = maps.Categories[categoryId],
            }).ToList(),
        }));
        context.ForegroundApplications.AddRange(data.Applications.Select(item => new ForegroundApplicationRow
        {
            Id = mode == RestoreMode.Merge ? Guid.CreateVersion7() : item.Id,
            SessionId = maps.Sessions[item.SessionId],
            DisplayName = item.DisplayName,
            ExecutableFileName = item.ExecutableFileName,
            DurationMilliseconds = item.DurationMilliseconds,
        }));
        context.Palettes.AddRange(data.Palettes.Select(item => new PaletteRow
        {
            Id = mode == RestoreMode.Merge ? Guid.CreateVersion7() : item.Id,
            Name = item.Name,
            FormatVersion = item.FormatVersion,
            IsBuiltIn = item.IsBuiltIn,
            LightCanvas = item.LightCanvas,
            LightSurface = item.LightSurface,
            LightAccent = item.LightAccent,
            DarkCanvas = item.DarkCanvas,
            DarkSurface = item.DarkSurface,
            DarkAccent = item.DarkAccent,
        }));
    }

    private sealed record RestoreMaps(
        IReadOnlyDictionary<Guid, Guid> Streams,
        IReadOnlyDictionary<Guid, Guid> Categories,
        IReadOnlyDictionary<Guid, Guid> Projects,
        IReadOnlyDictionary<Guid, Guid> Sessions,
        IReadOnlyDictionary<Guid, Guid> Adjustments)
    {
        public static RestoreMaps Identity(PortableData data) => new(
            data.Streams.ToDictionary(item => item.Id, item => item.Id),
            data.Categories.ToDictionary(item => item.Id, item => item.Id),
            data.Projects.ToDictionary(item => item.Id, item => item.Id),
            data.Sessions.ToDictionary(item => item.Id, item => item.Id),
            data.Adjustments.ToDictionary(item => item.Id, item => item.Id));

        public static RestoreMaps Remap(PortableData data, IReadOnlySet<Guid> existing) => new(
            Map(data.Streams.Select(item => item.Id), existing),
            Map(data.Categories.Select(item => item.Id), existing),
            Map(data.Projects.Select(item => item.Id), existing),
            Map(data.Sessions.Select(item => item.Id), existing),
            Map(data.Adjustments.Select(item => item.Id), existing));

        private static Dictionary<Guid, Guid> Map(IEnumerable<Guid> ids, IReadOnlySet<Guid> existing) =>
            ids.ToDictionary(id => id, id => existing.Contains(id) ? Guid.CreateVersion7() : id);
    }
}
