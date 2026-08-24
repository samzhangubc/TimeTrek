using TimeTrek.Domain;

namespace TimeTrek.Domain.Tests;

public sealed class DomainAssemblyTests
{
    [Fact]
    public void MarkerResolvesDomainAssembly()
    {
        Assert.Equal("TimeTrek.Domain", typeof(DomainAssembly).Assembly.GetName().Name);
    }
}
