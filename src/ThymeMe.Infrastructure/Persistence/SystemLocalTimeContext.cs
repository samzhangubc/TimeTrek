using ThymeMe.Application.Timing;

namespace ThymeMe.Infrastructure.Persistence;

public sealed class SystemLocalTimeContext : ILocalTimeContext
{
    public string TimeZoneId => TimeZoneInfo.Local.Id;

    public int GetUtcOffsetMinutes(long utcMilliseconds)
    {
        DateTimeOffset utc = DateTimeOffset.FromUnixTimeMilliseconds(utcMilliseconds);
        return checked((int)TimeZoneInfo.Local.GetUtcOffset(utc).TotalMinutes);
    }
}
