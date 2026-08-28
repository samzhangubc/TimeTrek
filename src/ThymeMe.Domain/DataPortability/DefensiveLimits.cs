namespace ThymeMe.Domain.DataPortability;

public static class DefensiveLimits
{
    public const int MaximumOrganizationObjects = 10_000;
    public const int MaximumSessionRecords = 1_000_000;
    public const int MaximumActivityRecords = 5_000_000;
    public const int MaximumJsonDepth = 32;
    public const int MaximumArchiveEntries = 64;
    public const long MaximumInputBytes = 2L * 1024 * 1024 * 1024;
    public const long MaximumExpandedBytes = 4L * 1024 * 1024 * 1024;
    public const int MaximumCompressionRatio = 200;
    public const int PageSize = 200;
}
