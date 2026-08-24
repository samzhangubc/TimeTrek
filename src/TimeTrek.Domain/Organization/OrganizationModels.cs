using TimeTrek.Domain.Common;

namespace TimeTrek.Domain.Organization;

public enum BudgetResetPeriod
{
    None,
    Weekly,
    Monthly,
}

public sealed record TimeBudget(long DurationMilliseconds, BudgetResetPeriod ResetPeriod)
{
    public TimeBudget Validate()
    {
        if (DurationMilliseconds <= 0)
        {
            throw new DomainValidationException("A time budget must be positive.");
        }

        return this;
    }
}

public sealed record StreamDefinition(
    Guid Id,
    string Name,
    string? Color,
    int SortOrder,
    TimeBudget? Budget,
    bool IsArchived,
    long CreatedUtcMilliseconds,
    long UpdatedUtcMilliseconds)
{
    public static StreamDefinition Create(string name, string? color, int sortOrder, long nowUtcMilliseconds) =>
        new(
            Guid.CreateVersion7(),
            DomainText.RequiredName(name, nameof(name)),
            ColorValue.NormalizeOptional(color),
            sortOrder,
            null,
            false,
            nowUtcMilliseconds,
            nowUtcMilliseconds);
}

public sealed record CategoryDefinition(
    Guid Id,
    string Name,
    Guid? StreamId,
    bool DefaultBillable,
    long? HourlyRateMinorUnits,
    string? CurrencyCode,
    bool IsArchived,
    long CreatedUtcMilliseconds,
    long UpdatedUtcMilliseconds)
{
    public bool IsGlobal => StreamId is null;

    public static CategoryDefinition Create(
        string name,
        Guid? streamId,
        bool defaultBillable,
        long? hourlyRateMinorUnits,
        string? currencyCode,
        long nowUtcMilliseconds)
    {
        Billing.BillingPolicy.ValidateRate(defaultBillable, hourlyRateMinorUnits, currencyCode);
        return new(
            Guid.CreateVersion7(),
            DomainText.RequiredName(name, nameof(name)),
            streamId,
            defaultBillable,
            hourlyRateMinorUnits,
            Billing.BillingPolicy.NormalizeCurrency(currencyCode),
            false,
            nowUtcMilliseconds,
            nowUtcMilliseconds);
    }
}

public sealed record ProjectDefinition(
    Guid Id,
    string Name,
    Guid? StreamId,
    TimeBudget? Budget,
    bool IsArchived,
    long CreatedUtcMilliseconds,
    long UpdatedUtcMilliseconds)
{
    public static ProjectDefinition Create(string name, Guid? streamId, long nowUtcMilliseconds) =>
        new(
            Guid.CreateVersion7(),
            DomainText.RequiredName(name, nameof(name)),
            streamId,
            null,
            false,
            nowUtcMilliseconds,
            nowUtcMilliseconds);
}

public sealed record SessionAssociations(Guid? StreamId, IReadOnlySet<Guid> CategoryIds, Guid? ProjectId)
{
    public static SessionAssociations Empty { get; } = new(null, new HashSet<Guid>(), null);

    public static SessionAssociations Create(Guid? streamId, IEnumerable<Guid>? categoryIds, Guid? projectId)
    {
        HashSet<Guid> categories = categoryIds?.ToHashSet() ?? [];
        if (categories.Count > 64)
        {
            throw new DomainValidationException("A Session cannot contain more than 64 Categories.");
        }

        return new(streamId, categories, projectId);
    }
}

public static class ColorValue
{
    public static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 7 || normalized[0] != '#')
        {
            throw new DomainValidationException("Colors must use #RRGGBB format.");
        }

        for (int index = 1; index < normalized.Length; index++)
        {
            if (!Uri.IsHexDigit(normalized[index]))
            {
                throw new DomainValidationException("Colors must use #RRGGBB format.");
            }
        }

        return normalized;
    }
}
