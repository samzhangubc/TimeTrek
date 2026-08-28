using ThymeMe.Domain.Common;

namespace ThymeMe.Domain.Reporting;

public enum RoundingRule
{
    None,
    Nearest,
}

public sealed record RoundingSnapshot(long RawMilliseconds, long EffectiveMilliseconds, int? IncrementMinutes, RoundingRule Rule);

public static class RoundingPolicy
{
    public static RoundingSnapshot Apply(long rawMilliseconds, int? incrementMinutes, RoundingRule rule)
    {
        if (incrementMinutes is null || rule == RoundingRule.None)
        {
            return new(rawMilliseconds, rawMilliseconds, null, RoundingRule.None);
        }

        if (incrementMinutes is < 1 or > 60)
        {
            throw new DomainValidationException("Rounding increment must be between 1 and 60 minutes.");
        }

        long incrementMilliseconds = checked(incrementMinutes.Value * 60_000L);
        long sign = Math.Sign(rawMilliseconds);
        decimal units = Math.Abs((decimal)rawMilliseconds) / incrementMilliseconds;
        long roundedUnits = checked((long)decimal.Round(units, 0, MidpointRounding.ToEven));
        long effective = checked(sign * roundedUnits * incrementMilliseconds);
        return new(rawMilliseconds, effective, incrementMinutes, rule);
    }
}
