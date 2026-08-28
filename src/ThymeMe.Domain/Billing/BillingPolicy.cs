using System.Globalization;
using ThymeMe.Domain.Common;

namespace ThymeMe.Domain.Billing;

public sealed record BillingSnapshot(
    bool IsBillable,
    long? HourlyRateMinorUnits,
    string? CurrencyCode,
    long? EstimatedEarningMinorUnits)
{
    public static BillingSnapshot Create(
        bool isBillable,
        long? hourlyRateMinorUnits,
        string? currencyCode,
        long effectiveDurationMilliseconds)
    {
        BillingPolicy.ValidateRate(isBillable, hourlyRateMinorUnits, currencyCode);
        string? normalizedCurrency = BillingPolicy.NormalizeCurrency(currencyCode);
        long? earning = isBillable && hourlyRateMinorUnits is long rate
            ? BillingPolicy.CalculateEarning(rate, effectiveDurationMilliseconds)
            : null;
        return new(isBillable, hourlyRateMinorUnits, normalizedCurrency, earning);
    }
}

public static class BillingPolicy
{
    public static string? NormalizeCurrency(string? currencyCode)
    {
        if (currencyCode is null)
        {
            return null;
        }

        string normalized = currencyCode.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(character => character is < 'A' or > 'Z'))
        {
            throw new DomainValidationException("Currency must be a three-letter ISO 4217 code.");
        }

        return normalized;
    }

    public static void ValidateRate(bool isBillable, long? hourlyRateMinorUnits, string? currencyCode)
    {
        if (hourlyRateMinorUnits is < 0)
        {
            throw new DomainValidationException("Hourly rate cannot be negative.");
        }

        if (hourlyRateMinorUnits is not null && string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new DomainValidationException("A currency is required when an hourly rate is set.");
        }

        if (currencyCode is not null)
        {
            _ = NormalizeCurrency(currencyCode);
        }

        if (!isBillable && hourlyRateMinorUnits is not null)
        {
            throw new DomainValidationException("A non-billable value cannot carry an hourly rate.");
        }
    }

    public static long CalculateEarning(long hourlyRateMinorUnits, long effectiveDurationMilliseconds)
    {
        if (hourlyRateMinorUnits < 0)
        {
            throw new DomainValidationException("Hourly rates cannot be negative.");
        }

        decimal amount = (decimal)hourlyRateMinorUnits * effectiveDurationMilliseconds / 3_600_000m;
        decimal rounded = decimal.Round(amount, 0, MidpointRounding.ToEven);

        try
        {
            return checked((long)rounded);
        }
        catch (OverflowException exception)
        {
            throw new DomainValidationException(
                string.Format(CultureInfo.InvariantCulture, "Calculated earning is outside the supported range: {0}", exception.Message));
        }
    }
}
