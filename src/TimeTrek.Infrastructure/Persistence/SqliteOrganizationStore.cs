using Microsoft.EntityFrameworkCore;
using TimeTrek.Application.Organization;
using TimeTrek.Domain.Organization;

namespace TimeTrek.Infrastructure.Persistence;

public sealed class SqliteOrganizationStore(IDbContextFactory<TimeTrekDbContext> contextFactory) : IOrganizationStore
{
    public async ValueTask<IReadOnlyList<StreamDefinition>> ListStreamsAsync(
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        await using TimeTrekDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        IQueryable<StreamRow> query = context.Streams.AsNoTracking().Where(row => !row.IsDeleted);
        if (!includeArchived)
        {
            query = query.Where(row => !row.IsArchived);
        }

        List<StreamRow> rows = await query.OrderBy(row => row.SortOrder).ThenBy(row => row.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(ToDomain).ToList();
    }

    public async ValueTask<IReadOnlyList<CategoryDefinition>> ListCategoriesAsync(
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        await using TimeTrekDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        IQueryable<CategoryRow> query = context.Categories.AsNoTracking().Where(row => !row.IsDeleted);
        if (!includeArchived)
        {
            query = query.Where(row => !row.IsArchived);
        }

        List<CategoryRow> rows = await query.OrderBy(row => row.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(ToDomain).ToList();
    }

    public async ValueTask<IReadOnlyList<ProjectDefinition>> ListProjectsAsync(
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        await using TimeTrekDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        IQueryable<ProjectRow> query = context.Projects.AsNoTracking().Where(row => !row.IsDeleted);
        if (!includeArchived)
        {
            query = query.Where(row => !row.IsArchived);
        }

        List<ProjectRow> rows = await query.OrderBy(row => row.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(ToDomain).ToList();
    }

    public async ValueTask AddStreamAsync(StreamDefinition stream, CancellationToken cancellationToken = default)
    {
        await using TimeTrekDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        context.Streams.Add(new StreamRow
        {
            Id = stream.Id,
            Name = stream.Name,
            Color = stream.Color,
            SortOrder = stream.SortOrder,
            BudgetMilliseconds = stream.Budget?.DurationMilliseconds,
            BudgetResetPeriod = (int)(stream.Budget?.ResetPeriod ?? BudgetResetPeriod.None),
            IsArchived = stream.IsArchived,
            CreatedUtcMilliseconds = stream.CreatedUtcMilliseconds,
            UpdatedUtcMilliseconds = stream.UpdatedUtcMilliseconds,
        });
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask AddCategoryAsync(CategoryDefinition category, CancellationToken cancellationToken = default)
    {
        await using TimeTrekDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        context.Categories.Add(new CategoryRow
        {
            Id = category.Id,
            Name = category.Name,
            StreamId = category.StreamId,
            DefaultBillable = category.DefaultBillable,
            HourlyRateMinorUnits = category.HourlyRateMinorUnits,
            CurrencyCode = category.CurrencyCode,
            IsArchived = category.IsArchived,
            CreatedUtcMilliseconds = category.CreatedUtcMilliseconds,
            UpdatedUtcMilliseconds = category.UpdatedUtcMilliseconds,
        });
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask AddProjectAsync(ProjectDefinition project, CancellationToken cancellationToken = default)
    {
        await using TimeTrekDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        context.Projects.Add(new ProjectRow
        {
            Id = project.Id,
            Name = project.Name,
            StreamId = project.StreamId,
            BudgetMilliseconds = project.Budget?.DurationMilliseconds,
            BudgetResetPeriod = (int)(project.Budget?.ResetPeriod ?? BudgetResetPeriod.None),
            IsArchived = project.IsArchived,
            CreatedUtcMilliseconds = project.CreatedUtcMilliseconds,
            UpdatedUtcMilliseconds = project.UpdatedUtcMilliseconds,
        });
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public ValueTask ArchiveAsync(
        OrganizationKind kind,
        Guid id,
        long archivedUtcMilliseconds,
        CancellationToken cancellationToken = default) =>
        SetArchivedAsync(kind, id, true, archivedUtcMilliseconds, cancellationToken);

    public ValueTask RestoreAsync(
        OrganizationKind kind,
        Guid id,
        long restoredUtcMilliseconds,
        CancellationToken cancellationToken = default) =>
        SetArchivedAsync(kind, id, false, restoredUtcMilliseconds, cancellationToken);

    public async ValueTask<AssociationContext> GetAssociationContextAsync(
        SessionAssociations associations,
        CancellationToken cancellationToken = default)
    {
        await using TimeTrekDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        Guid[] categoryIds = associations.CategoryIds.ToArray();
        List<CategoryRow> categoryRows = await context.Categories.AsNoTracking()
            .Where(row => categoryIds.Contains(row.Id) && !row.IsDeleted)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        List<ProjectRow> projectRows = associations.ProjectId is Guid projectId
            ? await context.Projects.AsNoTracking()
                .Where(row => row.Id == projectId && !row.IsDeleted)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false)
            : [];

        return new(
            categoryRows.Select(ToDomain).ToDictionary(category => category.Id),
            projectRows.Select(ToDomain).ToDictionary(project => project.Id));
    }

    private async ValueTask SetArchivedAsync(
        OrganizationKind kind,
        Guid id,
        bool archived,
        long nowUtcMilliseconds,
        CancellationToken cancellationToken)
    {
        await using TimeTrekDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        bool activeConflict = await context.TimingRoots.AnyAsync(
            row => row.StreamId == id || row.ProjectId == id,
            cancellationToken).ConfigureAwait(false);
        if (activeConflict)
        {
            throw new InvalidOperationException("Organization used by the active Session cannot be archived yet.");
        }

        switch (kind)
        {
            case OrganizationKind.Stream:
                StreamRow stream = await context.Streams.SingleAsync(row => row.Id == id && !row.IsDeleted, cancellationToken).ConfigureAwait(false);
                stream.IsArchived = archived;
                stream.ArchivedUtcMilliseconds = archived ? nowUtcMilliseconds : null;
                stream.UpdatedUtcMilliseconds = nowUtcMilliseconds;

                if (archived)
                {
                    await context.Categories.Where(row => row.StreamId == id && !row.IsArchived && !row.IsDeleted)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(row => row.IsArchived, true)
                            .SetProperty(row => row.ArchivedUtcMilliseconds, nowUtcMilliseconds)
                            .SetProperty(row => row.UpdatedUtcMilliseconds, nowUtcMilliseconds), cancellationToken)
                        .ConfigureAwait(false);
                    await context.Projects.Where(row => row.StreamId == id && !row.IsArchived && !row.IsDeleted)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(row => row.IsArchived, true)
                            .SetProperty(row => row.ArchivedUtcMilliseconds, nowUtcMilliseconds)
                            .SetProperty(row => row.UpdatedUtcMilliseconds, nowUtcMilliseconds), cancellationToken)
                        .ConfigureAwait(false);
                }

                break;
            case OrganizationKind.Category:
                CategoryRow category = await context.Categories.SingleAsync(row => row.Id == id && !row.IsDeleted, cancellationToken).ConfigureAwait(false);
                category.IsArchived = archived;
                category.ArchivedUtcMilliseconds = archived ? nowUtcMilliseconds : null;
                category.UpdatedUtcMilliseconds = nowUtcMilliseconds;
                break;
            case OrganizationKind.Project:
                ProjectRow project = await context.Projects.SingleAsync(row => row.Id == id && !row.IsDeleted, cancellationToken).ConfigureAwait(false);
                project.IsArchived = archived;
                project.ArchivedUtcMilliseconds = archived ? nowUtcMilliseconds : null;
                project.UpdatedUtcMilliseconds = nowUtcMilliseconds;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static StreamDefinition ToDomain(StreamRow row) => new(
        row.Id,
        row.Name,
        row.Color,
        row.SortOrder,
        row.BudgetMilliseconds is long duration
            ? new TimeBudget(duration, (BudgetResetPeriod)row.BudgetResetPeriod)
            : null,
        row.IsArchived,
        row.CreatedUtcMilliseconds,
        row.UpdatedUtcMilliseconds);

    private static CategoryDefinition ToDomain(CategoryRow row) => new(
        row.Id,
        row.Name,
        row.StreamId,
        row.DefaultBillable,
        row.HourlyRateMinorUnits,
        row.CurrencyCode,
        row.IsArchived,
        row.CreatedUtcMilliseconds,
        row.UpdatedUtcMilliseconds);

    private static ProjectDefinition ToDomain(ProjectRow row) => new(
        row.Id,
        row.Name,
        row.StreamId,
        row.BudgetMilliseconds is long duration
            ? new TimeBudget(duration, (BudgetResetPeriod)row.BudgetResetPeriod)
            : null,
        row.IsArchived,
        row.CreatedUtcMilliseconds,
        row.UpdatedUtcMilliseconds);
}
