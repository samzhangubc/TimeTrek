using System.Diagnostics;
using System.Runtime.InteropServices;
using TimeTrek.Application.Lifecycle;

namespace TimeTrek.Platform.Windows.ActivityTracking;

public sealed partial class WindowsForegroundApplicationSource : IForegroundApplicationSource
{
    private CancellationTokenSource? cancellation;
    private Task? pollingTask;
    private uint lastProcessId;

    public event EventHandler<ForegroundApplicationChangedEvent>? Changed;

    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        if (pollingTask is not null)
        {
            return ValueTask.CompletedTask;
        }

        this.cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        pollingTask = PollAsync(this.cancellation.Token);
        return ValueTask.CompletedTask;
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        if (this.cancellation is null)
        {
            return;
        }

        await this.cancellation.CancelAsync().ConfigureAwait(false);
        if (pollingTask is not null)
        {
            try
            {
                await pollingTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        this.cancellation.Dispose();
        this.cancellation = null;
        pollingTask = null;
        lastProcessId = 0;
    }

    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(2));
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            nint window = GetForegroundWindow();
            if (window == 0)
            {
                continue;
            }

            _ = GetWindowThreadProcessId(window, out uint processId);
            if (processId == 0 || processId == lastProcessId)
            {
                continue;
            }

            lastProcessId = processId;
            string displayName;
            string executable;
            bool inaccessible = false;
            try
            {
                using Process process = Process.GetProcessById(checked((int)processId));
                displayName = string.IsNullOrWhiteSpace(process.MainWindowTitle) ? process.ProcessName : process.ProcessName;
                executable = $"{process.ProcessName}.exe";
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                displayName = "Unknown elevated application";
                executable = "unknown-elevated";
                inaccessible = true;
            }

            Changed?.Invoke(this, new ForegroundApplicationChangedEvent(
                displayName,
                executable,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                inaccessible));
        }
    }

    [LibraryImport("user32.dll")]
    private static partial nint GetForegroundWindow();

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(nint window, out uint processId);
}
