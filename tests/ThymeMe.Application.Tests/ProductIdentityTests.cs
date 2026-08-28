using ThymeMe.Application;

namespace ThymeMe.Application.Tests;

public sealed class ProductIdentityTests
{
    [Theory]
    [InlineData("Thyme-Me")]
    [InlineData("TimeTrek")]
    public void CurrentAndLegacyBackupProductNamesRemainSupported(string product) =>
        Assert.True(ProductIdentity.IsSupportedBackupProduct(product));

    [Fact]
    public void UnknownBackupProductNameIsRejected() =>
        Assert.False(ProductIdentity.IsSupportedBackupProduct("AnotherProduct"));
}
