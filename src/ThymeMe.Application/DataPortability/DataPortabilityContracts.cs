namespace ThymeMe.Application.DataPortability;

public enum ExportFormat
{
    CsvBundle,
    Json,
}

public enum RestoreMode
{
    Replace,
    Merge,
}

public sealed record ExportRequest(ExportFormat Format, bool IncludeApplicationBreakdown, History.HistoryQuery? Filter);

public sealed record RestorePreview(
    int StreamCount,
    int CategoryCount,
    int ProjectCount,
    int SessionCount,
    int RemappedConflictCount,
    string SchemaVersion);

public interface IDataPortabilityService
{
    ValueTask ExportAsync(Stream destination, ExportRequest request, CancellationToken cancellationToken = default);

    ValueTask CreateBackupAsync(Stream destination, CancellationToken cancellationToken = default);

    ValueTask<RestorePreview> PreviewRestoreAsync(Stream source, RestoreMode mode, CancellationToken cancellationToken = default);

    ValueTask RestoreAsync(Stream source, RestoreMode mode, CancellationToken cancellationToken = default);
}

public interface IUserFileDialogService
{
    ValueTask InitializeAsync(nint windowHandle, CancellationToken cancellationToken = default);

    ValueTask<string?> PickSavePathAsync(string suggestedFileName, string displayName, string extension, CancellationToken cancellationToken = default);

    ValueTask<string?> PickOpenPathAsync(string displayName, IReadOnlyList<string> extensions, CancellationToken cancellationToken = default);
}
