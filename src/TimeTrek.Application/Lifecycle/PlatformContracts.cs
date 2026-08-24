namespace TimeTrek.Application.Lifecycle;

public enum InterruptionKind
{
    Idle,
    DisplayOff,
    ScreenSaver,
    Locked,
    Suspended,
}

public sealed record InterruptionEvent(InterruptionKind Kind, long DetectedUtcMilliseconds);

public interface INotificationService
{
    ValueTask ShowAsync(string title, string message, string? activationArgument = null, CancellationToken cancellationToken = default);
}

public interface ITrayService : IAsyncDisposable
{
    event EventHandler? RestoreRequested;

    ValueTask InitializeAsync(nint windowHandle, string iconPath, CancellationToken cancellationToken = default);

    ValueTask ShowAsync(CancellationToken cancellationToken = default);

    ValueTask HideAsync(CancellationToken cancellationToken = default);

    ValueTask UpdateTimerAsync(TimeSpan elapsed, CancellationToken cancellationToken = default);
}

public interface IInterruptionSource : IAsyncDisposable
{
    event EventHandler<InterruptionEvent>? Interrupted;

    ValueTask InitializeAsync(nint windowHandle, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    ValueTask StartAsync(CancellationToken cancellationToken = default);
}

public interface IForegroundApplicationSource : IAsyncDisposable
{
    event EventHandler<ForegroundApplicationChangedEvent>? Changed;

    ValueTask StartAsync(CancellationToken cancellationToken = default);

    ValueTask StopAsync(CancellationToken cancellationToken = default);
}

public sealed record ForegroundApplicationChangedEvent(string DisplayName, string ExecutableFileName, long ChangedUtcMilliseconds, bool IsElevatedUnknown);

public interface IStartupRegistrationService
{
    ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken = default);

    ValueTask SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
}

public interface IClipboardService
{
    ValueTask SetTextAsync(string text, CancellationToken cancellationToken = default);
}

public interface ICalendarIntegration
{
    string ProviderName { get; }

    bool IsAvailable { get; }
}
