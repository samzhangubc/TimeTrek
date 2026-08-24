using TimeTrek.Application.Common;
using TimeTrek.Application.Organization;
using TimeTrek.Application.Timing;
using TimeTrek.Domain.Billing;
using TimeTrek.Domain.Common;
using TimeTrek.Domain.Organization;
using TimeTrek.Domain.Reporting;
using TimeTrek.Domain.Timing;

namespace TimeTrek.Application.History;

public sealed record AddManualSessionRequest(
    long StartUtcMilliseconds,
    long EndUtcMilliseconds,
    SessionAssociations Associations,
    bool IsBillable,
    long? HourlyRateMinorUnits,
    string? CurrencyCode,
    int? RoundingIncrementMinutes,
    RoundingRule RoundingRule,
    string? Description,
    bool AllowFuture,
    SessionOrigin Origin = SessionOrigin.Manual);

public sealed record AddAdjustmentRequest(
    long LocalDateUnixDays,
    long SignedDurationMilliseconds,
    SessionAssociations Associations,
    bool IsBillable,
    long? HourlyRateMinorUnits,
    string? CurrencyCode,
    int? RoundingIncrementMinutes,
    RoundingRule RoundingRule,
    string? Description);

public sealed class HistoryService(
    IHistoryStore store,
    IOrganizationStore organizationStore,
    ILocalTimeContext localTime,
    TimeProvider timeProvider)
{
    public ValueTask<HistoryPage> QueryAsync(HistoryQuery query, CancellationToken cancellationToken = default) =>
        store.QueryAsync(query, cancellationToken);

    public async ValueTask<OperationResult<CompletedSession>> AddManualAsync(
        AddManualSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.EndUtcMilliseconds <= request.StartUtcMilliseconds)
            {
                return OperationResult.Failure<CompletedSession>("history.time_range", "End time must follow start time.");
            }

            long now = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
            if (!request.AllowFuture && request.EndUtcMilliseconds > now)
            {
                return OperationResult.Failure<CompletedSession>("history.future_confirmation", "This Session extends into the future and requires confirmation.");
            }

            AssociationContext context = await organizationStore.GetAssociationContextAsync(request.Associations, cancellationToken).ConfigureAwait(false);
            AssociationPolicy.Validate(request.Associations, context.Categories, context.Projects);
            long raw = checked(request.EndUtcMilliseconds - request.StartUtcMilliseconds);
            RoundingSnapshot duration = RoundingPolicy.Apply(raw, request.RoundingIncrementMinutes, request.RoundingRule);
            BillingSnapshot billing = BillingSnapshot.Create(
                request.IsBillable,
                request.HourlyRateMinorUnits,
                request.CurrencyCode,
                duration.EffectiveMilliseconds);
            CompletedSession session = new(
                Guid.CreateVersion7(),
                request.Origin,
                TimingMode.Normal,
                request.Associations,
                request.StartUtcMilliseconds,
                request.EndUtcMilliseconds,
                localTime.TimeZoneId,
                localTime.GetUtcOffsetMinutes(request.StartUtcMilliseconds),
                localTime.GetUtcOffsetMinutes(request.EndUtcMilliseconds),
                duration,
                raw,
                billing,
                DomainText.OptionalDescription(request.Description),
                true,
                now,
                now);
            await store.AddManualAsync(session, cancellationToken).ConfigureAwait(false);
            return OperationResult.Success(session);
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure<CompletedSession>("history.invalid", exception.Message);
        }
    }

    public async ValueTask<OperationResult<CompletedSession>> ContinueAsync(
        Guid sourceSessionId,
        long startUtcMilliseconds,
        long endUtcMilliseconds,
        bool allowFuture,
        CancellationToken cancellationToken = default)
    {
        CompletedSession? source = await store.GetSessionAsync(sourceSessionId, cancellationToken).ConfigureAwait(false);
        if (source is null)
        {
            return OperationResult.Failure<CompletedSession>("history.not_found", "The source Session no longer exists.");
        }

        return await AddManualAsync(new AddManualSessionRequest(
            startUtcMilliseconds,
            endUtcMilliseconds,
            source.Associations,
            source.Billing.IsBillable,
            source.Billing.HourlyRateMinorUnits,
            source.Billing.CurrencyCode,
            null,
            RoundingRule.None,
            null,
            allowFuture,
            SessionOrigin.Continued), cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<OperationResult<NegativeAdjustment>> AddAdjustmentAsync(
        AddAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.SignedDurationMilliseconds >= 0)
            {
                return OperationResult.Failure<NegativeAdjustment>("adjustment.sign", "A negative adjustment must reduce time.");
            }

            AssociationContext context = await organizationStore.GetAssociationContextAsync(request.Associations, cancellationToken).ConfigureAwait(false);
            AssociationPolicy.Validate(request.Associations, context.Categories, context.Projects);
            RoundingSnapshot duration = RoundingPolicy.Apply(
                request.SignedDurationMilliseconds,
                request.RoundingIncrementMinutes,
                request.RoundingRule);
            BillingSnapshot billing = BillingSnapshot.Create(
                request.IsBillable,
                request.HourlyRateMinorUnits,
                request.CurrencyCode,
                duration.EffectiveMilliseconds);
            long now = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
            NegativeAdjustment adjustment = new(
                Guid.CreateVersion7(),
                request.Associations,
                request.LocalDateUnixDays,
                localTime.TimeZoneId,
                duration,
                billing,
                DomainText.OptionalDescription(request.Description),
                now);
            await store.AddAdjustmentAsync(adjustment, cancellationToken).ConfigureAwait(false);
            return OperationResult.Success(adjustment);
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure<NegativeAdjustment>("adjustment.invalid", exception.Message);
        }
    }

    public async ValueTask DeleteAsync(Guid id, int retentionDays, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(retentionDays, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(retentionDays, 365);
        long deleted = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        long purge = checked(deleted + retentionDays * 86_400_000L);
        await store.SoftDeleteAsync(id, deleted, purge, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask RestoreAsync(Guid id, CancellationToken cancellationToken = default) =>
        store.RestoreAsync(id, cancellationToken);

    public async ValueTask<OperationResult<bool>> UpdateDescriptionAsync(
        Guid id,
        string? description,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string? validated = DomainText.OptionalDescription(description);
            await store.UpdateDescriptionAsync(
                id,
                validated,
                timeProvider.GetUtcNow().ToUnixTimeMilliseconds(),
                cancellationToken).ConfigureAwait(false);
            return OperationResult.Success(true);
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure<bool>("history.description", exception.Message);
        }
    }
}
