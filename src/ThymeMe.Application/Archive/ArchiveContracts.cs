using ThymeMe.Application.Organization;

namespace ThymeMe.Application.Archive;

public sealed record DeletionPreview(
    OrganizationKind RootKind,
    Guid RootId,
    string RootName,
    int OrganizationCount,
    int SessionCount,
    bool RequiresTypedName);

public sealed record DeletedBatch(
    Guid Id,
    string RootName,
    OrganizationKind RootKind,
    long DeletedUtcMilliseconds,
    long PurgeAfterUtcMilliseconds);

public interface IArchiveStore
{
    ValueTask<DeletionPreview> PreviewDeletionAsync(OrganizationKind kind, Guid id, CancellationToken cancellationToken = default);

    ValueTask<Guid> DeleteCascadeAsync(
        DeletionPreview preview,
        string? typedName,
        long deletedUtcMilliseconds,
        long purgeAfterUtcMilliseconds,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<DeletedBatch>> ListDeletedBatchesAsync(CancellationToken cancellationToken = default);

    ValueTask RestoreBatchAsync(Guid batchId, CancellationToken cancellationToken = default);

    ValueTask PurgeExpiredBatchesAsync(long nowUtcMilliseconds, CancellationToken cancellationToken = default);
}
