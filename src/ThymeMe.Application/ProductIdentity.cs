namespace ThymeMe.Application;

/// <summary>Centralizes the public product identity.</summary>
public static class ProductIdentity
{
    public const string DisplayName = "Thyme-Me";
    public const string RepositoryUrl = "https://github.com/samzhangubc/Thyme-Me";
    // Retained only so backups created by the pre-rename release remain restorable.
    public const string LegacyBackupProductName = "TimeTrek";

    public static bool IsSupportedBackupProduct(string product) =>
        product is DisplayName or LegacyBackupProductName;
}
