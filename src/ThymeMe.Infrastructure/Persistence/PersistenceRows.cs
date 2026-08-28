namespace ThymeMe.Infrastructure.Persistence;

internal sealed class StreamRow
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Color { get; set; }

    public int SortOrder { get; set; }

    public long? BudgetMilliseconds { get; set; }

    public int BudgetResetPeriod { get; set; }

    public bool IsArchived { get; set; }

    public long? ArchivedUtcMilliseconds { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeletedUtcMilliseconds { get; set; }

    public long? PurgeAfterUtcMilliseconds { get; set; }

    public Guid? DeletionBatchId { get; set; }

    public long CreatedUtcMilliseconds { get; set; }

    public long UpdatedUtcMilliseconds { get; set; }
}

internal sealed class CategoryRow
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public Guid? StreamId { get; set; }

    public bool DefaultBillable { get; set; }

    public long? HourlyRateMinorUnits { get; set; }

    public string? CurrencyCode { get; set; }

    public bool IsArchived { get; set; }

    public long? ArchivedUtcMilliseconds { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeletedUtcMilliseconds { get; set; }

    public long? PurgeAfterUtcMilliseconds { get; set; }

    public Guid? DeletionBatchId { get; set; }

    public long CreatedUtcMilliseconds { get; set; }

    public long UpdatedUtcMilliseconds { get; set; }
}

internal sealed class ProjectRow
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public Guid? StreamId { get; set; }

    public long? BudgetMilliseconds { get; set; }

    public int BudgetResetPeriod { get; set; }

    public bool IsArchived { get; set; }

    public long? ArchivedUtcMilliseconds { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeletedUtcMilliseconds { get; set; }

    public long? PurgeAfterUtcMilliseconds { get; set; }

    public Guid? DeletionBatchId { get; set; }

    public long CreatedUtcMilliseconds { get; set; }

    public long UpdatedUtcMilliseconds { get; set; }
}

internal sealed class SessionRow
{
    public Guid Id { get; set; }

    public int Origin { get; set; }

    public int TimingMode { get; set; }

    public bool IsDraft { get; set; }

    public Guid? StreamId { get; set; }

    public Guid? ProjectId { get; set; }

    public long StartUtcMilliseconds { get; set; }

    public long EndUtcMilliseconds { get; set; }

    public required string TimeZoneId { get; set; }

    public int StartUtcOffsetMinutes { get; set; }

    public int EndUtcOffsetMinutes { get; set; }

    public long RawDurationMilliseconds { get; set; }

    public long EffectiveDurationMilliseconds { get; set; }

    public int? RoundingIncrementMinutes { get; set; }

    public int RoundingRule { get; set; }

    public long RequestedDurationMilliseconds { get; set; }

    public string? Description { get; set; }

    public bool CompletionPending { get; set; }

    public bool IsBillable { get; set; }

    public long? HourlyRateMinorUnits { get; set; }

    public string? CurrencyCode { get; set; }

    public long? EstimatedEarningMinorUnits { get; set; }

    public string? StreamOriginalName { get; set; }

    public string? ProjectOriginalName { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeletedUtcMilliseconds { get; set; }

    public long? PurgeAfterUtcMilliseconds { get; set; }

    public Guid? DeletionBatchId { get; set; }

    public long CreatedUtcMilliseconds { get; set; }

    public long UpdatedUtcMilliseconds { get; set; }

    public ICollection<SessionCategoryRow> Categories { get; set; } = [];
}

internal sealed class SessionCategoryRow
{
    public Guid SessionId { get; set; }

    public Guid CategoryId { get; set; }

    public string? OriginalName { get; set; }

    public SessionRow? Session { get; set; }
}

internal sealed class TimingRootRow
{
    public Guid Id { get; set; }

    public int SingletonKey { get; set; } = 1;

    public Guid SessionId { get; set; }

    public int Status { get; set; }

    public int TimingMode { get; set; }

    public Guid? StreamId { get; set; }

    public Guid? ProjectId { get; set; }

    public required string CategoryIdsJson { get; set; }

    public bool IsBillable { get; set; }

    public long? HourlyRateMinorUnits { get; set; }

    public string? CurrencyCode { get; set; }

    public int? RoundingIncrementMinutes { get; set; }

    public int RoundingRule { get; set; }

    public long StartedUtcMilliseconds { get; set; }

    public long CurrentSegmentStartedUtcMilliseconds { get; set; }

    public long RequestedDurationMilliseconds { get; set; }

    public long ScheduledEndUtcMilliseconds { get; set; }

    public long AccumulatedWorkMilliseconds { get; set; }

    public long AccumulatedBreakMilliseconds { get; set; }

    public long? PomodoroTotalMilliseconds { get; set; }

    public long? PomodoroWorkMilliseconds { get; set; }

    public long? PomodoroBreakMilliseconds { get; set; }

    public long? PomodoroBufferMilliseconds { get; set; }

    public bool PomodoroBreaksBillable { get; set; }

    public int? PausedFromStatus { get; set; }

    public int Revision { get; set; }
}

internal sealed class TimingSegmentRow
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public int Kind { get; set; }

    public long StartUtcMilliseconds { get; set; }

    public long EndUtcMilliseconds { get; set; }
}

internal sealed class AdjustmentRow
{
    public Guid Id { get; set; }

    public Guid? StreamId { get; set; }

    public Guid? ProjectId { get; set; }

    public long LocalDateUnixDays { get; set; }

    public required string TimeZoneId { get; set; }

    public long RawDurationMilliseconds { get; set; }

    public long EffectiveDurationMilliseconds { get; set; }

    public int? RoundingIncrementMinutes { get; set; }

    public int RoundingRule { get; set; }

    public bool IsBillable { get; set; }

    public long? HourlyRateMinorUnits { get; set; }

    public string? CurrencyCode { get; set; }

    public long? EstimatedEarningMinorUnits { get; set; }

    public string? Description { get; set; }

    public bool IsDeleted { get; set; }

    public long? DeletedUtcMilliseconds { get; set; }

    public long? PurgeAfterUtcMilliseconds { get; set; }

    public Guid? DeletionBatchId { get; set; }

    public long CreatedUtcMilliseconds { get; set; }

    public ICollection<AdjustmentCategoryRow> Categories { get; set; } = [];
}

internal sealed class AdjustmentCategoryRow
{
    public Guid AdjustmentId { get; set; }

    public Guid CategoryId { get; set; }

    public string? OriginalName { get; set; }

    public AdjustmentRow? Adjustment { get; set; }
}

internal sealed class ForegroundApplicationRow
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public required string DisplayName { get; set; }

    public required string ExecutableFileName { get; set; }

    public long DurationMilliseconds { get; set; }
}

internal sealed class PaletteRow
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public int FormatVersion { get; set; }

    public bool IsBuiltIn { get; set; }

    public required string LightCanvas { get; set; }

    public required string LightSurface { get; set; }

    public required string LightAccent { get; set; }

    public required string DarkCanvas { get; set; }

    public required string DarkSurface { get; set; }

    public required string DarkAccent { get; set; }
}

internal sealed class SettingRow
{
    public required string Key { get; set; }

    public required string JsonValue { get; set; }

    public int SchemaVersion { get; set; }

    public long UpdatedUtcMilliseconds { get; set; }
}

internal sealed class DeletionBatchRow
{
    public Guid Id { get; set; }

    public int RootKind { get; set; }

    public Guid RootId { get; set; }

    public required string RootName { get; set; }

    public long DeletedUtcMilliseconds { get; set; }

    public long PurgeAfterUtcMilliseconds { get; set; }
}

internal sealed class SchemaStateRow
{
    public int Id { get; set; } = 1;

    public int SchemaVersion { get; set; }

    public bool CleanShutdown { get; set; }

    public int ConsecutiveStartupFailures { get; set; }

    public long UpdatedUtcMilliseconds { get; set; }
}
