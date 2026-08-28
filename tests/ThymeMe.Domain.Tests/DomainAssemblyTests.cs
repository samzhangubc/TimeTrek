using ThymeMe.Domain;

namespace ThymeMe.Domain.Tests;

public sealed class DomainAssemblyTests
{
    [Fact]
    public void MarkerResolvesDomainAssembly()
    {
        Assert.Equal("ThymeMe.Domain", typeof(DomainAssembly).Assembly.GetName().Name);
    }
}
