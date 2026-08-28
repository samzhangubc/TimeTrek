using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using ThymeMe.Application.Lifecycle;
using ThymeMe.Application.Settings;
using ThymeMe.Application.Timing;
using ThymeMe.Domain.Timing;

namespace ThymeMe.Application.ActivityTracking;

public sealed class ActivityTrackingCoordinator(
    IForegroundApplicationSource source,
    IActivityStore store,
    ITimingStore timingStore,
    IAppSettingsStore settingsStore) : BackgroundService
{
    private readonly Channel<ForegroundApplicationChangedEvent> events = Channel.CreateBounded<ForegroundApplicationChangedEvent>(
        new BoundedChannelOptions(128)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        AppSettings settings = await settingsStore.LoadAsync(stoppingToken).ConfigureAwait(false);
        if (!settings.ForegroundTrackingEnabled)
        {
            return;
        }

        source.Changed += OnChanged;
        await source.StartAsync(stoppingToken).ConfigureAwait(false);
        ForegroundApplicationChangedEvent? previous = null;
        try
        {
            await foreach (ForegroundApplicationChangedEvent current in events.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                if (previous is not null)
                {
                    TimingSnapshot? timing = await timingStore.GetActiveAsync(stoppingToken).ConfigureAwait(false);
                    long duration = current.ChangedUtcMilliseconds - previous.ChangedUtcMilliseconds;
                    if (timing is not null && timing.Status is TimingStatus.ActiveWork or TimingStatus.BreakPending &&
                        duration is > 0 and <= 60_000)
                    {
                        await store.AddDurationAsync(
                            timing.SessionId,
                            previous.DisplayName,
                            previous.ExecutableFileName,
                            duration,
                            stoppingToken).ConfigureAwait(false);
                    }
                }

                previous = current;
            }
        }
        finally
        {
            source.Changed -= OnChanged;
            await source.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private void OnChanged(object? sender, ForegroundApplicationChangedEvent e) => events.Writer.TryWrite(e);
}
