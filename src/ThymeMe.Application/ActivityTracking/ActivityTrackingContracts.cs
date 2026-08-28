namespace ThymeMe.Application.ActivityTracking;

public sealed record ApplicationDuration(string DisplayName, string ExecutableFileName, long DurationMilliseconds);

public interface IActivityStore
{
    ValueTask AddDurationAsync(
        Guid sessionId,
        string displayName,
        string executableFileName,
        long durationMilliseconds,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<ApplicationDuration>> GetForSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    ValueTask PurgeAllAsync(CancellationToken cancellationToken = default);
}
