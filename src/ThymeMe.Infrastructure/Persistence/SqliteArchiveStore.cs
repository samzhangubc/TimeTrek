using Microsoft.EntityFrameworkCore;
using ThymeMe.Application.Archive;
using ThymeMe.Application.Organization;

namespace ThymeMe.Infrastructure.Persistence;

public sealed class SqliteArchiveStore(IDbContextFactory<ThymeMeDbContext> contextFactory) : IArchiveStore
{
    public async ValueTask<DeletionPreview> PreviewDeletionAsync(
        OrganizationKind kind,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        string name;
        int organizations = 1;
        int sessions;
        switch (kind)
        {
            case OrganizationKind.Stream:
                name = await context.Streams.Where(row => row.Id == id && !row.IsDeleted).Select(row => row.Name).SingleAsync(cancellationToken).ConfigureAwait(false);
                organizations += await context.Categories.CountAsync(row => row.StreamId == id && !row.IsDeleted, cancellationToken).ConfigureAwait(false);
                organizations += await context.Projects.CountAsync(row => row.StreamId == id && !row.IsDeleted, cancellationToken).ConfigureAwait(false);
                sessions = await context.Sessions.CountAsync(row => !row.IsDeleted && (row.StreamId == id || row.ProjectId != null && context.Projects.Any(project => project.Id == row.ProjectId && project.StreamId == id) || row.Categories.Any(join => context.Categories.Any(category => category.Id == join.CategoryId && category.StreamId == id))), cancellationToken).ConfigureAwait(false);
                break;
            case OrganizationKind.Category:
                name = await context.Categories.Where(row => row.Id == id && !row.IsDeleted).Select(row => row.Name).SingleAsync(cancellationToken).ConfigureAwait(false);
                sessions = await context.SessionCategories.CountAsync(row => row.CategoryId == id, cancellationToken).ConfigureAwait(false);
                break;
            case OrganizationKind.Project:
                name = await context.Projects.Where(row => row.Id == id && !row.IsDeleted).Select(row => row.Name).SingleAsync(cancellationToken).ConfigureAwait(false);
                sessions = await context.Sessions.CountAsync(row => row.ProjectId == id && !row.IsDeleted, cancellationToken).ConfigureAwait(false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }

        return new(kind, id, name, organizations, sessions, organizations >= 5 || sessions >= 25);
    }

    public async ValueTask<Guid> DeleteCascadeAsync(
        DeletionPreview preview,
        string? typedName,
        long deletedUtcMilliseconds,
        long purgeAfterUtcMilliseconds,
        CancellationToken cancellationToken = default)
    {
        if (preview.RequiresTypedName && !string.Equals(preview.RootName, typedName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The exact root name is required for this large cascade.");
        }

        DeletionPreview current = await PreviewDeletionAsync(preview.RootKind, preview.RootId, cancellationToken).ConfigureAwait(false);
        if (current != preview)
        {
            throw new InvalidOperationException("The cascade changed after preview. Review it again before deleting.");
        }

        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        Guid batchId = Guid.CreateVersion7();
        context.DeletionBatches.Add(new DeletionBatchRow
        {
            Id = batchId,
            RootKind = (int)preview.RootKind,
            RootId = preview.RootId,
            RootName = preview.RootName,
            DeletedUtcMilliseconds = deletedUtcMilliseconds,
            PurgeAfterUtcMilliseconds = purgeAfterUtcMilliseconds,
        });

        IQueryable<SessionRow> affectedSessions = context.Sessions.Where(row => !row.IsDeleted);
        switch (preview.RootKind)
        {
            case OrganizationKind.Stream:
                await context.Streams.Where(row => row.Id == preview.RootId).ExecuteUpdateAsync(set => set.SetProperty(row => row.IsDeleted, true).SetProperty(row => row.DeletedUtcMilliseconds, deletedUtcMilliseconds).SetProperty(row => row.PurgeAfterUtcMilliseconds, purgeAfterUtcMilliseconds).SetProperty(row => row.DeletionBatchId, batchId), cancellationToken).ConfigureAwait(false);
                await context.Categories.Where(row => row.StreamId == preview.RootId && !row.IsDeleted).ExecuteUpdateAsync(set => set.SetProperty(row => row.IsDeleted, true).SetProperty(row => row.DeletedUtcMilliseconds, deletedUtcMilliseconds).SetProperty(row => row.PurgeAfterUtcMilliseconds, purgeAfterUtcMilliseconds).SetProperty(row => row.DeletionBatchId, batchId), cancellationToken).ConfigureAwait(false);
                await context.Projects.Where(row => row.StreamId == preview.RootId && !row.IsDeleted).ExecuteUpdateAsync(set => set.SetProperty(row => row.IsDeleted, true).SetProperty(row => row.DeletedUtcMilliseconds, deletedUtcMilliseconds).SetProperty(row => row.PurgeAfterUtcMilliseconds, purgeAfterUtcMilliseconds).SetProperty(row => row.DeletionBatchId, batchId), cancellationToken).ConfigureAwait(false);
                affectedSessions = affectedSessions.Where(row => row.StreamId == preview.RootId || row.ProjectId != null && context.Projects.Any(project => project.Id == row.ProjectId && project.StreamId == preview.RootId) || row.Categories.Any(join => context.Categories.Any(category => category.Id == join.CategoryId && category.StreamId == preview.RootId)));
                break;
            case OrganizationKind.Category:
                await context.Categories.Where(row => row.Id == preview.RootId).ExecuteUpdateAsync(set => set.SetProperty(row => row.IsDeleted, true).SetProperty(row => row.DeletedUtcMilliseconds, deletedUtcMilliseconds).SetProperty(row => row.PurgeAfterUtcMilliseconds, purgeAfterUtcMilliseconds).SetProperty(row => row.DeletionBatchId, batchId), cancellationToken).ConfigureAwait(false);
                affectedSessions = affectedSessions.Where(row => row.Categories.Any(join => join.CategoryId == preview.RootId));
                break;
            case OrganizationKind.Project:
                await context.Projects.Where(row => row.Id == preview.RootId).ExecuteUpdateAsync(set => set.SetProperty(row => row.IsDeleted, true).SetProperty(row => row.DeletedUtcMilliseconds, deletedUtcMilliseconds).SetProperty(row => row.PurgeAfterUtcMilliseconds, purgeAfterUtcMilliseconds).SetProperty(row => row.DeletionBatchId, batchId), cancellationToken).ConfigureAwait(false);
                affectedSessions = affectedSessions.Where(row => row.ProjectId == preview.RootId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(preview));
        }

        await affectedSessions.ExecuteUpdateAsync(set => set
            .SetProperty(row => row.IsDeleted, true)
            .SetProperty(row => row.DeletedUtcMilliseconds, deletedUtcMilliseconds)
            .SetProperty(row => row.PurgeAfterUtcMilliseconds, purgeAfterUtcMilliseconds)
            .SetProperty(row => row.DeletionBatchId, batchId), cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return batchId;
    }

    public async ValueTask<IReadOnlyList<DeletedBatch>> ListDeletedBatchesAsync(CancellationToken cancellationToken = default)
    {
        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await context.DeletionBatches.AsNoTracking().OrderByDescending(row => row.DeletedUtcMilliseconds)
            .Select(row => new DeletedBatch(row.Id, row.RootName, (OrganizationKind)row.RootKind, row.DeletedUtcMilliseconds, row.PurgeAfterUtcMilliseconds))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask RestoreBatchAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await context.Streams.Where(row => row.DeletionBatchId == batchId).ExecuteUpdateAsync(set => set.SetProperty(row => row.IsDeleted, false).SetProperty(row => row.DeletedUtcMilliseconds, (long?)null).SetProperty(row => row.PurgeAfterUtcMilliseconds, (long?)null).SetProperty(row => row.DeletionBatchId, (Guid?)null), cancellationToken).ConfigureAwait(false);
        await context.Categories.Where(row => row.DeletionBatchId == batchId).ExecuteUpdateAsync(set => set.SetProperty(row => row.IsDeleted, false).SetProperty(row => row.DeletedUtcMilliseconds, (long?)null).SetProperty(row => row.PurgeAfterUtcMilliseconds, (long?)null).SetProperty(row => row.DeletionBatchId, (Guid?)null), cancellationToken).ConfigureAwait(false);
        await context.Projects.Where(row => row.DeletionBatchId == batchId).ExecuteUpdateAsync(set => set.SetProperty(row => row.IsDeleted, false).SetProperty(row => row.DeletedUtcMilliseconds, (long?)null).SetProperty(row => row.PurgeAfterUtcMilliseconds, (long?)null).SetProperty(row => row.DeletionBatchId, (Guid?)null), cancellationToken).ConfigureAwait(false);
        await context.Sessions.Where(row => row.DeletionBatchId == batchId).ExecuteUpdateAsync(set => set.SetProperty(row => row.IsDeleted, false).SetProperty(row => row.DeletedUtcMilliseconds, (long?)null).SetProperty(row => row.PurgeAfterUtcMilliseconds, (long?)null).SetProperty(row => row.DeletionBatchId, (Guid?)null), cancellationToken).ConfigureAwait(false);
        await context.DeletionBatches.Where(row => row.Id == batchId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask PurgeExpiredBatchesAsync(long nowUtcMilliseconds, CancellationToken cancellationToken = default)
    {
        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        Guid[] batches = await context.DeletionBatches.Where(row => row.PurgeAfterUtcMilliseconds <= nowUtcMilliseconds).Select(row => row.Id).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        if (batches.Length == 0)
        {
            return;
        }

        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await context.SessionCategories.Where(row => context.Sessions.Any(session => session.Id == row.SessionId && session.DeletionBatchId != null && batches.Contains(session.DeletionBatchId.Value))).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.Sessions.Where(row => row.DeletionBatchId != null && batches.Contains(row.DeletionBatchId.Value)).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.Categories.Where(row => row.DeletionBatchId != null && batches.Contains(row.DeletionBatchId.Value)).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.Projects.Where(row => row.DeletionBatchId != null && batches.Contains(row.DeletionBatchId.Value)).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.Streams.Where(row => row.DeletionBatchId != null && batches.Contains(row.DeletionBatchId.Value)).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.DeletionBatches.Where(row => batches.Contains(row.Id)).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

}
