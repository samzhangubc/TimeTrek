using TimeTrek.Application.Settings;

namespace TimeTrek.Infrastructure.DataPortability;

internal sealed record PortableData(
    string SchemaVersion,
    DateTimeOffset CreatedUtc,
    IReadOnlyList<PortableStream> Streams,
    IReadOnlyList<PortableCategory> Categories,
    IReadOnlyList<PortableProject> Projects,
    IReadOnlyList<PortableSession> Sessions,
    IReadOnlyList<PortableAdjustment> Adjustments,
    IReadOnlyList<PortableApplication> Applications,
    IReadOnlyList<PortablePalette> Palettes,
    AppSettings Settings);

internal sealed record PortableStream(
    Guid Id,
    string Name,
    string? Color,
    int SortOrder,
    long? BudgetMilliseconds,
    int BudgetResetPeriod,
    bool IsArchived,
    long? ArchivedUtcMilliseconds,
    long CreatedUtcMilliseconds,
    long UpdatedUtcMilliseconds);

internal sealed record PortableCategory(
    Guid Id,
    string Name,
    Guid? StreamId,
    bool DefaultBillable,
    long? HourlyRateMinorUnits,
    string? CurrencyCode,
    bool IsArchived,
    long? ArchivedUtcMilliseconds,
    long CreatedUtcMilliseconds,
    long UpdatedUtcMilliseconds);

internal sealed record PortableProject(
    Guid Id,
    string Name,
    Guid? StreamId,
    long? BudgetMilliseconds,
    int BudgetResetPeriod,
    bool IsArchived,
    long? ArchivedUtcMilliseconds,
    long CreatedUtcMilliseconds,
    long UpdatedUtcMilliseconds);

internal sealed record PortableSession(
    Guid Id,
    int Origin,
    int TimingMode,
    Guid? StreamId,
    Guid? ProjectId,
    IReadOnlyList<Guid> CategoryIds,
    long StartUtcMilliseconds,
    long EndUtcMilliseconds,
    string TimeZoneId,
    int StartUtcOffsetMinutes,
    int EndUtcOffsetMinutes,
    long RawDurationMilliseconds,
    long EffectiveDurationMilliseconds,
    int? RoundingIncrementMinutes,
    int RoundingRule,
    long RequestedDurationMilliseconds,
    string? Description,
    bool CompletionPending,
    bool IsBillable,
    long? HourlyRateMinorUnits,
    string? CurrencyCode,
    long? EstimatedEarningMinorUnits,
    long CreatedUtcMilliseconds,
    long UpdatedUtcMilliseconds);

internal sealed record PortableAdjustment(
    Guid Id,
    Guid? StreamId,
    Guid? ProjectId,
    IReadOnlyList<Guid> CategoryIds,
    long LocalDateUnixDays,
    string TimeZoneId,
    long RawDurationMilliseconds,
    long EffectiveDurationMilliseconds,
    int? RoundingIncrementMinutes,
    int RoundingRule,
    bool IsBillable,
    long? HourlyRateMinorUnits,
    string? CurrencyCode,
    long? EstimatedEarningMinorUnits,
    string? Description,
    long CreatedUtcMilliseconds);

internal sealed record PortableApplication(
    Guid Id,
    Guid SessionId,
    string DisplayName,
    string ExecutableFileName,
    long DurationMilliseconds);

internal sealed record PortablePalette(
    Guid Id,
    string Name,
    int FormatVersion,
    bool IsBuiltIn,
    string LightCanvas,
    string LightSurface,
    string LightAccent,
    string DarkCanvas,
    string DarkSurface,
    string DarkAccent);

internal sealed record BackupManifest(
    string Product,
    string SchemaVersion,
    DateTimeOffset CreatedUtc,
    string DataEntry,
    long DataBytes,
    string Sha256);
