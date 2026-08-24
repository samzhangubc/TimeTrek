using TimeTrek.Application.Common;
using TimeTrek.Domain.Common;
using TimeTrek.Domain.Organization;

namespace TimeTrek.Application.Organization;

public sealed class OrganizationService(IOrganizationStore store, TimeProvider timeProvider)
{
    public ValueTask<IReadOnlyList<StreamDefinition>> ListStreamsAsync(
        bool includeArchived = false,
        CancellationToken cancellationToken = default) =>
        store.ListStreamsAsync(includeArchived, cancellationToken);

    public async ValueTask<OperationResult<StreamDefinition>> CreateStreamAsync(
        string name,
        string? color = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IReadOnlyList<StreamDefinition> streams = await store.ListStreamsAsync(true, cancellationToken).ConfigureAwait(false);
            long now = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
            StreamDefinition stream = StreamDefinition.Create(name, color, streams.Count, now);
            await store.AddStreamAsync(stream, cancellationToken).ConfigureAwait(false);
            return OperationResult.Success(stream);
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure<StreamDefinition>("organization.invalid", exception.Message);
        }
    }

    public async ValueTask<OperationResult<CategoryDefinition>> CreateCategoryAsync(
        string name,
        Guid? streamId,
        bool defaultBillable,
        long? hourlyRateMinorUnits,
        string? currencyCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            long now = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
            CategoryDefinition category = CategoryDefinition.Create(
                name,
                streamId,
                defaultBillable,
                hourlyRateMinorUnits,
                currencyCode,
                now);
            await store.AddCategoryAsync(category, cancellationToken).ConfigureAwait(false);
            return OperationResult.Success(category);
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure<CategoryDefinition>("organization.invalid", exception.Message);
        }
    }

    public async ValueTask<OperationResult<ProjectDefinition>> CreateProjectAsync(
        string name,
        Guid? streamId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            long now = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
            ProjectDefinition project = ProjectDefinition.Create(name, streamId, now);
            await store.AddProjectAsync(project, cancellationToken).ConfigureAwait(false);
            return OperationResult.Success(project);
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure<ProjectDefinition>("organization.invalid", exception.Message);
        }
    }

    public ValueTask ArchiveAsync(
        OrganizationKind kind,
        Guid id,
        CancellationToken cancellationToken = default) =>
        store.ArchiveAsync(kind, id, timeProvider.GetUtcNow().ToUnixTimeMilliseconds(), cancellationToken);
}
