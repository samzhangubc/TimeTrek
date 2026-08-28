using ThymeMe.Domain.Timing;

namespace ThymeMe.Application.History;

public enum HistorySortField
{
    Date,
    Duration,
    Stream,
    Category,
    Project,
}

public sealed record HistoryQuery(
    string? Search = null,
    IReadOnlySet<Guid>? StreamIds = null,
    IReadOnlySet<Guid>? CategoryIds = null,
    IReadOnlySet<Guid>? ProjectIds = null,
    long? MinimumDurationMilliseconds = null,
    long? MaximumDurationMilliseconds = null,
    long? StartUtcMilliseconds = null,
    long? EndUtcMilliseconds = null,
    bool? IsBillable = null,
    IReadOnlySet<SessionOrigin>? Origins = null,
    bool IncludeDeleted = false,
    HistorySortField SortField = HistorySortField.Date,
    bool Descending = true,
    int Offset = 0,
    int Limit = 200);

public sealed record HistoryItem(
    Guid Id,
    long StartUtcMilliseconds,
    long EndUtcMilliseconds,
    long RawDurationMilliseconds,
    long EffectiveDurationMilliseconds,
    string? StreamName,
    IReadOnlyList<string> CategoryNames,
    string? ProjectName,
    string? Description,
    SessionOrigin Origin,
    bool IsBillable,
    long? EstimatedEarningMinorUnits,
    string? CurrencyCode,
    bool IsDeleted,
    long? PurgeAfterUtcMilliseconds);

public sealed record HistoryPage(IReadOnlyList<HistoryItem> Items, int TotalCount, long SignedTotalDurationMilliseconds);

public interface IHistoryStore
{
    ValueTask<HistoryPage> QueryAsync(HistoryQuery query, CancellationToken cancellationToken = default);

    ValueTask AddManualAsync(CompletedSession session, CancellationToken cancellationToken = default);

    ValueTask<CompletedSession?> GetSessionAsync(Guid id, CancellationToken cancellationToken = default);

    ValueTask AddAdjustmentAsync(NegativeAdjustment adjustment, CancellationToken cancellationToken = default);

    ValueTask SoftDeleteAsync(Guid id, long deletedUtcMilliseconds, long purgeAfterUtcMilliseconds, CancellationToken cancellationToken = default);

    ValueTask RestoreAsync(Guid id, CancellationToken cancellationToken = default);

    ValueTask UpdateDescriptionAsync(Guid id, string? description, long updatedUtcMilliseconds, CancellationToken cancellationToken = default);

    ValueTask PurgeExpiredAsync(long nowUtcMilliseconds, CancellationToken cancellationToken = default);
}
