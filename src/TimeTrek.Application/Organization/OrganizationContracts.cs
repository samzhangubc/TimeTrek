using TimeTrek.Domain.Organization;

namespace TimeTrek.Application.Organization;

public interface IOrganizationStore
{
    ValueTask<IReadOnlyList<StreamDefinition>> ListStreamsAsync(bool includeArchived, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<CategoryDefinition>> ListCategoriesAsync(bool includeArchived, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<ProjectDefinition>> ListProjectsAsync(bool includeArchived, CancellationToken cancellationToken = default);

    ValueTask AddStreamAsync(StreamDefinition stream, CancellationToken cancellationToken = default);

    ValueTask AddCategoryAsync(CategoryDefinition category, CancellationToken cancellationToken = default);

    ValueTask AddProjectAsync(ProjectDefinition project, CancellationToken cancellationToken = default);

    ValueTask ArchiveAsync(OrganizationKind kind, Guid id, long archivedUtcMilliseconds, CancellationToken cancellationToken = default);

    ValueTask RestoreAsync(OrganizationKind kind, Guid id, long restoredUtcMilliseconds, CancellationToken cancellationToken = default);

    ValueTask<AssociationContext> GetAssociationContextAsync(SessionAssociations associations, CancellationToken cancellationToken = default);
}

public enum OrganizationKind
{
    Stream,
    Category,
    Project,
}

public sealed record AssociationContext(
    IReadOnlyDictionary<Guid, CategoryDefinition> Categories,
    IReadOnlyDictionary<Guid, ProjectDefinition> Projects);
