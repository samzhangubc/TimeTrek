using TimeTrek.Application.Organization;

namespace TimeTrek.Application.Archive;

public sealed class ArchiveService(IArchiveStore store, TimeProvider timeProvider)
{
    public ValueTask<DeletionPreview> PreviewDeletionAsync(
        OrganizationKind kind,
        Guid id,
        CancellationToken cancellationToken = default) =>
        store.PreviewDeletionAsync(kind, id, cancellationToken);

    public async ValueTask<Guid> DeleteCascadeAsync(
        DeletionPreview preview,
        string? typedName,
        int retentionDays,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(retentionDays, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(retentionDays, 365);
        long deleted = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        return await store.DeleteCascadeAsync(
            preview,
            typedName,
            deleted,
            checked(deleted + retentionDays * 86_400_000L),
            cancellationToken).ConfigureAwait(false);
    }
}
