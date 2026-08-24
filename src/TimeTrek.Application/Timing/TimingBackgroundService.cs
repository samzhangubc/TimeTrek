using Microsoft.Extensions.Hosting;
using TimeTrek.Application.Lifecycle;

namespace TimeTrek.Application.Timing;

public sealed class TimingBackgroundService(TimingCoordinator coordinator, ITrayService trayService, TimeProvider timeProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            _ = await coordinator.AdvanceAutomaticAsync(stoppingToken).ConfigureAwait(false);
            TimeTrek.Domain.Timing.TimingSnapshot? active = await coordinator.GetActiveAsync(stoppingToken).ConfigureAwait(false);
            if (active is not null)
            {
                long now = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
                await trayService.UpdateTimerAsync(
                    TimeSpan.FromMilliseconds(active.ElapsedWorkAt(now)),
                    stoppingToken).ConfigureAwait(false);
            }
        }
    }
}
