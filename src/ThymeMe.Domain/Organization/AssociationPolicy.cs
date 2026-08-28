using ThymeMe.Domain.Common;

namespace ThymeMe.Domain.Organization;

public static class AssociationPolicy
{
    public static void Validate(
        SessionAssociations associations,
        IReadOnlyDictionary<Guid, CategoryDefinition> categories,
        IReadOnlyDictionary<Guid, ProjectDefinition> projects)
    {
        ArgumentNullException.ThrowIfNull(associations);
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(projects);

        foreach (Guid categoryId in associations.CategoryIds)
        {
            if (!categories.TryGetValue(categoryId, out CategoryDefinition? category))
            {
                throw new DomainValidationException("A selected Category does not exist.");
            }

            if (category.IsArchived)
            {
                throw new DomainValidationException("Archived Categories cannot be used for a new Session.");
            }

            if (category.StreamId is Guid parent && associations.StreamId != parent)
            {
                throw new DomainValidationException("A scoped Category requires its owning Stream.");
            }
        }

        if (associations.ProjectId is Guid projectId)
        {
            if (!projects.TryGetValue(projectId, out ProjectDefinition? project))
            {
                throw new DomainValidationException("The selected Project does not exist.");
            }

            if (project.IsArchived)
            {
                throw new DomainValidationException("An archived Project cannot be used for a new Session.");
            }

            if (project.StreamId is Guid owner && associations.StreamId != owner)
            {
                throw new DomainValidationException("An owned Project requires its owning Stream.");
            }
        }
    }
}
