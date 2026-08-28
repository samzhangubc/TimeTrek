using Microsoft.EntityFrameworkCore;
using ThymeMe.Application.History;
using ThymeMe.Domain.Timing;

namespace ThymeMe.Infrastructure.Persistence;

public sealed class SqliteHistoryStore(IDbContextFactory<ThymeMeDbContext> contextFactory) : IHistoryStore
{
    public async ValueTask<HistoryPage> QueryAsync(HistoryQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        int limit = Math.Clamp(query.Limit, 1, 500);
        int offset = Math.Max(0, query.Offset);
        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        IQueryable<SessionRow> source = context.Sessions.AsNoTracking().Where(row => !row.IsDraft);
        source = query.IncludeDeleted ? source.Where(row => row.IsDeleted) : source.Where(row => !row.IsDeleted);

        if (query.StreamIds is { Count: > 0 })
        {
            Guid[] ids = query.StreamIds.ToArray();
            source = source.Where(row => row.StreamId != null && ids.Contains(row.StreamId.Value));
        }

        if (query.ProjectIds is { Count: > 0 })
        {
            Guid[] ids = query.ProjectIds.ToArray();
            source = source.Where(row => row.ProjectId != null && ids.Contains(row.ProjectId.Value));
        }

        if (query.CategoryIds is { Count: > 0 })
        {
            Guid[] ids = query.CategoryIds.ToArray();
            source = source.Where(row => row.Categories.Any(category => ids.Contains(category.CategoryId)));
        }

        if (query.MinimumDurationMilliseconds is long minimum)
        {
            source = source.Where(row => row.RawDurationMilliseconds >= minimum);
        }

        if (query.MaximumDurationMilliseconds is long maximum)
        {
            source = source.Where(row => row.RawDurationMilliseconds <= maximum);
        }

        if (query.StartUtcMilliseconds is long start)
        {
            source = source.Where(row => row.EndUtcMilliseconds >= start);
        }

        if (query.EndUtcMilliseconds is long end)
        {
            source = source.Where(row => row.StartUtcMilliseconds < end);
        }

        if (query.IsBillable is bool isBillable)
        {
            source = source.Where(row => row.IsBillable == isBillable);
        }

        if (query.Origins is { Count: > 0 })
        {
            int[] origins = query.Origins.Select(origin => (int)origin).ToArray();
            source = source.Where(row => origins.Contains(row.Origin));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string escaped = EscapeLikePattern(query.Search.Trim());
            string pattern = $"%{escaped}%";
            source = source.Where(row =>
                (row.Description != null && EF.Functions.Like(row.Description, pattern, "\\")) ||
                (row.StreamId != null && context.Streams.Any(item => item.Id == row.StreamId && EF.Functions.Like(item.Name, pattern, "\\"))) ||
                (row.ProjectId != null && context.Projects.Any(item => item.Id == row.ProjectId && EF.Functions.Like(item.Name, pattern, "\\"))) ||
                row.Categories.Any(join => context.Categories.Any(item => item.Id == join.CategoryId && EF.Functions.Like(item.Name, pattern, "\\"))));
        }

        int totalCount = await source.CountAsync(cancellationToken).ConfigureAwait(false);
        long signedTotal = await source.Select(row => (long?)row.EffectiveDurationMilliseconds)
            .SumAsync(cancellationToken).ConfigureAwait(false) ?? 0;
        IOrderedQueryable<SessionRow> ordered = Order(source, query.SortField, query.Descending, context);
        List<SessionRow> rows = await ordered.Skip(offset).Take(limit)
            .Include(row => row.Categories)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Guid[] streamIds = rows.Where(row => row.StreamId.HasValue).Select(row => row.StreamId!.Value).Distinct().ToArray();
        Guid[] projectIds = rows.Where(row => row.ProjectId.HasValue).Select(row => row.ProjectId!.Value).Distinct().ToArray();
        Guid[] categoryIds = rows.SelectMany(row => row.Categories).Select(row => row.CategoryId).Distinct().ToArray();
        Dictionary<Guid, string> streamNames = await context.Streams.AsNoTracking().Where(row => streamIds.Contains(row.Id))
            .ToDictionaryAsync(row => row.Id, row => row.Name, cancellationToken).ConfigureAwait(false);
        Dictionary<Guid, string> projectNames = await context.Projects.AsNoTracking().Where(row => projectIds.Contains(row.Id))
            .ToDictionaryAsync(row => row.Id, row => row.Name, cancellationToken).ConfigureAwait(false);
        Dictionary<Guid, string> categoryNames = await context.Categories.AsNoTracking().Where(row => categoryIds.Contains(row.Id))
            .ToDictionaryAsync(row => row.Id, row => row.Name, cancellationToken).ConfigureAwait(false);

        List<HistoryItem> items = rows.Select(row => new HistoryItem(
            row.Id,
            row.StartUtcMilliseconds,
            row.EndUtcMilliseconds,
            row.RawDurationMilliseconds,
            row.EffectiveDurationMilliseconds,
            row.StreamId is Guid streamId && streamNames.TryGetValue(streamId, out string? streamName) ? streamName : null,
            row.Categories.Select(join => categoryNames.GetValueOrDefault(join.CategoryId, join.OriginalName ?? "Unknown Category")).Order().ToList(),
            row.ProjectId is Guid projectId && projectNames.TryGetValue(projectId, out string? projectName) ? projectName : null,
            row.Description,
            (SessionOrigin)row.Origin,
            row.IsBillable,
            row.EstimatedEarningMinorUnits,
            row.CurrencyCode,
            row.IsDeleted,
            row.PurgeAfterUtcMilliseconds)).ToList();

        IQueryable<AdjustmentRow> adjustmentSource = context.Adjustments.AsNoTracking();
        adjustmentSource = query.IncludeDeleted
            ? adjustmentSource.Where(row => row.IsDeleted)
            : adjustmentSource.Where(row => !row.IsDeleted);
        if (query.Origins is { Count: > 0 } && !query.Origins.Contains(SessionOrigin.Adjustment))
        {
            adjustmentSource = adjustmentSource.Where(_ => false);
        }

        if (query.StreamIds is { Count: > 0 })
        {
            Guid[] ids = query.StreamIds.ToArray();
            adjustmentSource = adjustmentSource.Where(row => row.StreamId != null && ids.Contains(row.StreamId.Value));
        }

        if (query.ProjectIds is { Count: > 0 })
        {
            Guid[] ids = query.ProjectIds.ToArray();
            adjustmentSource = adjustmentSource.Where(row => row.ProjectId != null && ids.Contains(row.ProjectId.Value));
        }

        if (query.CategoryIds is { Count: > 0 })
        {
            Guid[] ids = query.CategoryIds.ToArray();
            adjustmentSource = adjustmentSource.Where(row => row.Categories.Any(category => ids.Contains(category.CategoryId)));
        }

        if (query.IsBillable is bool adjustmentBillable)
        {
            adjustmentSource = adjustmentSource.Where(row => row.IsBillable == adjustmentBillable);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pattern = $"%{EscapeLikePattern(query.Search.Trim())}%";
            adjustmentSource = adjustmentSource.Where(row =>
                (row.Description != null && EF.Functions.Like(row.Description, pattern, "\\")) ||
                (row.StreamId != null && context.Streams.Any(item => item.Id == row.StreamId && EF.Functions.Like(item.Name, pattern, "\\"))) ||
                (row.ProjectId != null && context.Projects.Any(item => item.Id == row.ProjectId && EF.Functions.Like(item.Name, pattern, "\\"))) ||
                row.Categories.Any(join => context.Categories.Any(item => item.Id == join.CategoryId && EF.Functions.Like(item.Name, pattern, "\\"))));
        }

        int adjustmentCount = await adjustmentSource.CountAsync(cancellationToken).ConfigureAwait(false);
        long adjustmentTotal = await adjustmentSource.Select(row => (long?)row.EffectiveDurationMilliseconds)
            .SumAsync(cancellationToken).ConfigureAwait(false) ?? 0;
        List<AdjustmentRow> adjustments = await adjustmentSource.OrderByDescending(row => row.LocalDateUnixDays)
            .ThenByDescending(row => row.CreatedUtcMilliseconds)
            .Take(limit)
            .Include(row => row.Categories)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        Guid[] adjustmentStreamIds = adjustments.Where(row => row.StreamId.HasValue).Select(row => row.StreamId!.Value).Distinct().ToArray();
        Guid[] adjustmentProjectIds = adjustments.Where(row => row.ProjectId.HasValue).Select(row => row.ProjectId!.Value).Distinct().ToArray();
        Guid[] adjustmentCategoryIds = adjustments.SelectMany(row => row.Categories).Select(row => row.CategoryId).Distinct().ToArray();
        Dictionary<Guid, string> adjustmentStreamNames = await context.Streams.AsNoTracking().Where(row => adjustmentStreamIds.Contains(row.Id))
            .ToDictionaryAsync(row => row.Id, row => row.Name, cancellationToken).ConfigureAwait(false);
        Dictionary<Guid, string> adjustmentProjectNames = await context.Projects.AsNoTracking().Where(row => adjustmentProjectIds.Contains(row.Id))
            .ToDictionaryAsync(row => row.Id, row => row.Name, cancellationToken).ConfigureAwait(false);
        Dictionary<Guid, string> adjustmentCategoryNames = await context.Categories.AsNoTracking().Where(row => adjustmentCategoryIds.Contains(row.Id))
            .ToDictionaryAsync(row => row.Id, row => row.Name, cancellationToken).ConfigureAwait(false);
        items.AddRange(adjustments.Select(row => new HistoryItem(
            row.Id,
            checked(row.LocalDateUnixDays * 86_400_000L),
            checked(row.LocalDateUnixDays * 86_400_000L),
            row.RawDurationMilliseconds,
            row.EffectiveDurationMilliseconds,
            row.StreamId is Guid streamId && adjustmentStreamNames.TryGetValue(streamId, out string? streamName) ? streamName : null,
            row.Categories.Select(join => adjustmentCategoryNames.GetValueOrDefault(join.CategoryId, join.OriginalName ?? "Unknown Category")).Order().ToList(),
            row.ProjectId is Guid projectId && adjustmentProjectNames.TryGetValue(projectId, out string? projectName) ? projectName : null,
            row.Description,
            SessionOrigin.Adjustment,
            row.IsBillable,
            row.EstimatedEarningMinorUnits,
            row.CurrencyCode,
            row.IsDeleted,
            row.PurgeAfterUtcMilliseconds)));
        items = items.OrderByDescending(item => item.StartUtcMilliseconds).ThenByDescending(item => item.Id).Take(limit).ToList();

        return new(items, totalCount + adjustmentCount, checked(signedTotal + adjustmentTotal));
    }

    public async ValueTask AddManualAsync(CompletedSession session, CancellationToken cancellationToken = default)
    {
        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        context.Sessions.Add(ToRow(session));
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<CompletedSession?> GetSessionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        SessionRow? row = await context.Sessions.AsNoTracking().Include(item => item.Categories)
            .SingleOrDefaultAsync(item => item.Id == id && !item.IsDraft && !item.IsDeleted, cancellationToken)
            .ConfigureAwait(false);
        return row is null ? null : ToDomain(row);
    }

    public async ValueTask AddAdjustmentAsync(NegativeAdjustment adjustment, CancellationToken cancellationToken = default)
    {
        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        context.Adjustments.Add(new AdjustmentRow
        {
            Id = adjustment.Id,
            StreamId = adjustment.Associations.StreamId,
            ProjectId = adjustment.Associations.ProjectId,
            LocalDateUnixDays = adjustment.LocalDateUnixDays,
            TimeZoneId = adjustment.TimeZoneId,
            RawDurationMilliseconds = adjustment.Duration.RawMilliseconds,
            EffectiveDurationMilliseconds = adjustment.Duration.EffectiveMilliseconds,
            RoundingIncrementMinutes = adjustment.Duration.IncrementMinutes,
            RoundingRule = (int)adjustment.Duration.Rule,
            IsBillable = adjustment.Billing.IsBillable,
            HourlyRateMinorUnits = adjustment.Billing.HourlyRateMinorUnits,
            CurrencyCode = adjustment.Billing.CurrencyCode,
            EstimatedEarningMinorUnits = adjustment.Billing.EstimatedEarningMinorUnits,
            Description = adjustment.Description,
            CreatedUtcMilliseconds = adjustment.CreatedUtcMilliseconds,
            Categories = adjustment.Associations.CategoryIds.Select(categoryId => new AdjustmentCategoryRow
            {
                AdjustmentId = adjustment.Id,
                CategoryId = categoryId,
            }).ToList(),
        });
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask SoftDeleteAsync(
        Guid id,
        long deletedUtcMilliseconds,
        long purgeAfterUtcMilliseconds,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            purgeAfterUtcMilliseconds,
            deletedUtcMilliseconds);

        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        int changed = await context.Sessions.Where(row => row.Id == id && !row.IsDeleted && !row.IsDraft)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(row => row.IsDeleted, true)
                .SetProperty(row => row.DeletedUtcMilliseconds, deletedUtcMilliseconds)
                .SetProperty(row => row.PurgeAfterUtcMilliseconds, purgeAfterUtcMilliseconds), cancellationToken)
            .ConfigureAwait(false);
        if (changed != 1)
        {
            changed = await context.Adjustments.Where(row => row.Id == id && !row.IsDeleted)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(row => row.IsDeleted, true)
                    .SetProperty(row => row.DeletedUtcMilliseconds, deletedUtcMilliseconds)
                    .SetProperty(row => row.PurgeAfterUtcMilliseconds, purgeAfterUtcMilliseconds), cancellationToken)
                .ConfigureAwait(false);
            if (changed != 1)
            {
                throw new InvalidOperationException("The record could not be moved to Recently Deleted.");
            }
        }
    }

    public async ValueTask RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        int changed = await context.Sessions.Where(row => row.Id == id && row.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(row => row.IsDeleted, false)
                .SetProperty(row => row.DeletedUtcMilliseconds, (long?)null)
                .SetProperty(row => row.PurgeAfterUtcMilliseconds, (long?)null)
                .SetProperty(row => row.DeletionBatchId, (Guid?)null), cancellationToken)
            .ConfigureAwait(false);
        if (changed != 1)
        {
            changed = await context.Adjustments.Where(row => row.Id == id && row.IsDeleted)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(row => row.IsDeleted, false)
                    .SetProperty(row => row.DeletedUtcMilliseconds, (long?)null)
                    .SetProperty(row => row.PurgeAfterUtcMilliseconds, (long?)null)
                    .SetProperty(row => row.DeletionBatchId, (Guid?)null), cancellationToken)
                .ConfigureAwait(false);
            if (changed != 1)
            {
                throw new InvalidOperationException("The deleted record no longer exists.");
            }
        }
    }

    public async ValueTask UpdateDescriptionAsync(
        Guid id,
        string? description,
        long updatedUtcMilliseconds,
        CancellationToken cancellationToken = default)
    {
        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        int changed = await context.Sessions.Where(row => row.Id == id && !row.IsDraft && !row.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(row => row.Description, description)
                .SetProperty(row => row.UpdatedUtcMilliseconds, updatedUtcMilliseconds), cancellationToken)
            .ConfigureAwait(false);
        if (changed != 1)
        {
            changed = await context.Adjustments.Where(row => row.Id == id && !row.IsDeleted)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(row => row.Description, description), cancellationToken)
                .ConfigureAwait(false);
            if (changed != 1)
            {
                throw new InvalidOperationException("The record no longer exists.");
            }
        }
    }

    public async ValueTask PurgeExpiredAsync(long nowUtcMilliseconds, CancellationToken cancellationToken = default)
    {
        await using ThymeMeDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await context.Sessions.Where(row => row.IsDeleted && row.PurgeAfterUtcMilliseconds <= nowUtcMilliseconds)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.Adjustments.Where(row => row.IsDeleted && row.PurgeAfterUtcMilliseconds <= nowUtcMilliseconds)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private static IOrderedQueryable<SessionRow> Order(
        IQueryable<SessionRow> source,
        HistorySortField field,
        bool descending,
        ThymeMeDbContext context) => (field, descending) switch
        {
            (HistorySortField.Duration, false) => source.OrderBy(row => row.EffectiveDurationMilliseconds).ThenBy(row => row.Id),
            (HistorySortField.Duration, true) => source.OrderByDescending(row => row.EffectiveDurationMilliseconds).ThenByDescending(row => row.Id),
            (HistorySortField.Stream, false) => source.OrderBy(row => context.Streams.Where(item => item.Id == row.StreamId).Select(item => item.Name).FirstOrDefault()).ThenBy(row => row.StartUtcMilliseconds),
            (HistorySortField.Stream, true) => source.OrderByDescending(row => context.Streams.Where(item => item.Id == row.StreamId).Select(item => item.Name).FirstOrDefault()).ThenByDescending(row => row.StartUtcMilliseconds),
            (HistorySortField.Project, false) => source.OrderBy(row => context.Projects.Where(item => item.Id == row.ProjectId).Select(item => item.Name).FirstOrDefault()).ThenBy(row => row.StartUtcMilliseconds),
            (HistorySortField.Project, true) => source.OrderByDescending(row => context.Projects.Where(item => item.Id == row.ProjectId).Select(item => item.Name).FirstOrDefault()).ThenByDescending(row => row.StartUtcMilliseconds),
            (HistorySortField.Category, false) => source.OrderBy(row => row.Categories.Select(join => context.Categories.Where(item => item.Id == join.CategoryId).Select(item => item.Name).FirstOrDefault()).FirstOrDefault()).ThenBy(row => row.StartUtcMilliseconds),
            (HistorySortField.Category, true) => source.OrderByDescending(row => row.Categories.Select(join => context.Categories.Where(item => item.Id == join.CategoryId).Select(item => item.Name).FirstOrDefault()).FirstOrDefault()).ThenByDescending(row => row.StartUtcMilliseconds),
            (HistorySortField.Date, false) => source.OrderBy(row => row.StartUtcMilliseconds).ThenBy(row => row.Id),
            _ => source.OrderByDescending(row => row.StartUtcMilliseconds).ThenByDescending(row => row.Id),
        };

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    private static SessionRow ToRow(CompletedSession session) => new()
    {
        Id = session.Id,
        Origin = (int)session.Origin,
        TimingMode = (int)session.Mode,
        IsDraft = false,
        StreamId = session.Associations.StreamId,
        ProjectId = session.Associations.ProjectId,
        StartUtcMilliseconds = session.StartUtcMilliseconds,
        EndUtcMilliseconds = session.EndUtcMilliseconds,
        TimeZoneId = session.TimeZoneId,
        StartUtcOffsetMinutes = session.StartUtcOffsetMinutes,
        EndUtcOffsetMinutes = session.EndUtcOffsetMinutes,
        RawDurationMilliseconds = session.Duration.RawMilliseconds,
        EffectiveDurationMilliseconds = session.Duration.EffectiveMilliseconds,
        RoundingIncrementMinutes = session.Duration.IncrementMinutes,
        RoundingRule = (int)session.Duration.Rule,
        RequestedDurationMilliseconds = session.RequestedDurationMilliseconds,
        Description = session.Description,
        CompletionPending = session.CompletionPending,
        IsBillable = session.Billing.IsBillable,
        HourlyRateMinorUnits = session.Billing.HourlyRateMinorUnits,
        CurrencyCode = session.Billing.CurrencyCode,
        EstimatedEarningMinorUnits = session.Billing.EstimatedEarningMinorUnits,
        CreatedUtcMilliseconds = session.CreatedUtcMilliseconds,
        UpdatedUtcMilliseconds = session.UpdatedUtcMilliseconds,
        Categories = session.Associations.CategoryIds.Select(categoryId => new SessionCategoryRow
        {
            SessionId = session.Id,
            CategoryId = categoryId,
        }).ToList(),
    };

    private static CompletedSession ToDomain(SessionRow row) => new(
        row.Id,
        (SessionOrigin)row.Origin,
        (TimingMode)row.TimingMode,
        Domain.Organization.SessionAssociations.Create(row.StreamId, row.Categories.Select(item => item.CategoryId), row.ProjectId),
        row.StartUtcMilliseconds,
        row.EndUtcMilliseconds,
        row.TimeZoneId,
        row.StartUtcOffsetMinutes,
        row.EndUtcOffsetMinutes,
        new Domain.Reporting.RoundingSnapshot(
            row.RawDurationMilliseconds,
            row.EffectiveDurationMilliseconds,
            row.RoundingIncrementMinutes,
            (Domain.Reporting.RoundingRule)row.RoundingRule),
        row.RequestedDurationMilliseconds,
        new Domain.Billing.BillingSnapshot(
            row.IsBillable,
            row.HourlyRateMinorUnits,
            row.CurrencyCode,
            row.EstimatedEarningMinorUnits),
        row.Description,
        row.CompletionPending,
        row.CreatedUtcMilliseconds,
        row.UpdatedUtcMilliseconds);
}
