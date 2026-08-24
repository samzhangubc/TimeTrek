using TimeTrek.Application.Updates;

namespace TimeTrek.Platform.Windows.Updates;

public sealed class UnconfiguredUpdateService : IUpdateService
{
    public ValueTask<UpdateInfo?> CheckAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<UpdateInfo?>(null);
    }

    public ValueTask<string> DownloadAndVerifyAsync(
        UpdateInfo update,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromException<string>(new InvalidOperationException(
            "Production update metadata and publisher identity have not been configured by the release owner."));

    public ValueTask InstallAsync(string verifiedPackagePath, CancellationToken cancellationToken = default) =>
        ValueTask.FromException(new InvalidOperationException(
            "Production update installation is unavailable until release identity is configured."));
}

public sealed class DeferredCalendarIntegration : Application.Lifecycle.ICalendarIntegration
{
    public string ProviderName => "Google Calendar";

    public bool IsAvailable => false;
}
