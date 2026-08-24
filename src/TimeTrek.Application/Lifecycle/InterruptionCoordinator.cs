using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using TimeTrek.Application.Common;
using TimeTrek.Application.Settings;
using TimeTrek.Application.Timing;
using TimeTrek.Domain.Timing;

namespace TimeTrek.Application.Lifecycle;

public sealed class InterruptionCoordinator(
    IInterruptionSource source,
    IAppSettingsStore settingsStore,
    TimingCoordinator timingCoordinator,
    INotificationService notifications) : BackgroundService
{
    private readonly Channel<InterruptionEvent> events = Channel.CreateBounded<InterruptionEvent>(
        new BoundedChannelOptions(16)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        source.Interrupted += OnInterrupted;
        await source.StartAsync(stoppingToken).ConfigureAwait(false);
        try
        {
            await foreach (InterruptionEvent interruption in events.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                AppSettings settings = await settingsStore.LoadAsync(stoppingToken).ConfigureAwait(false);
                if (!Enabled(settings, interruption.Kind) || await timingCoordinator.GetActiveAsync(stoppingToken).ConfigureAwait(false) is null)
                {
                    continue;
                }

                OperationResult<CompletedSession> stopped = await timingCoordinator.StopAsync(stoppingToken).ConfigureAwait(false);
                if (stopped.IsSuccess)
                {
                    await notifications.ShowAsync(
                        "Session stopped",
                        $"TimeTrek stopped the Session after {interruption.Kind} was detected. Completion is waiting in the app.",
                        "open-completion",
                        stoppingToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            source.Interrupted -= OnInterrupted;
        }
    }

    private void OnInterrupted(object? sender, InterruptionEvent e) => events.Writer.TryWrite(e);

    private static bool Enabled(AppSettings settings, InterruptionKind kind) =>
        settings.InterruptionHandlingEnabled && kind switch
        {
            InterruptionKind.Idle => settings.StopOnGenericInactivity,
            InterruptionKind.DisplayOff => settings.StopOnDisplayOff,
            InterruptionKind.ScreenSaver => settings.StopOnScreenSaver,
            InterruptionKind.Locked => settings.StopOnLock,
            InterruptionKind.Suspended => settings.StopOnSuspend,
            _ => false,
        };
}
