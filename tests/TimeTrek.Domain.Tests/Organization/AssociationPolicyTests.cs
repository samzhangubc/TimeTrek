using TimeTrek.Domain.Common;
using TimeTrek.Domain.Organization;

namespace TimeTrek.Domain.Tests.Organization;

public sealed class AssociationPolicyTests
{
    [Fact]
    public void ScopedCategoryRequiresOwningStream()
    {
        Guid streamId = Guid.CreateVersion7();
        CategoryDefinition category = CategoryDefinition.Create("Lecture", streamId, false, null, null, 1);
        SessionAssociations associations = SessionAssociations.Create(null, [category.Id], null);

        Assert.Throws<DomainValidationException>(() => AssociationPolicy.Validate(
            associations,
            new Dictionary<Guid, CategoryDefinition> { [category.Id] = category },
            new Dictionary<Guid, ProjectDefinition>()));
    }

    [Fact]
    public void GlobalCategoryCanBeUsedWithOrWithoutStream()
    {
        CategoryDefinition category = CategoryDefinition.Create("Exercise", null, false, null, null, 1);
        Dictionary<Guid, CategoryDefinition> categories = new() { [category.Id] = category };

        AssociationPolicy.Validate(
            SessionAssociations.Create(null, [category.Id], null),
            categories,
            new Dictionary<Guid, ProjectDefinition>());
        AssociationPolicy.Validate(
            SessionAssociations.Create(Guid.CreateVersion7(), [category.Id], null),
            categories,
            new Dictionary<Guid, ProjectDefinition>());
    }
}
