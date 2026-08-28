using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using ThymeMe.Application.Lifecycle;

namespace ThymeMe.Platform.Windows.Lifecycle;

public sealed class WindowsNotificationService : INotificationService
{
    public ValueTask ShowAsync(
        string title,
        string message,
        string? activationArgument = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AppNotificationBuilder builder = new AppNotificationBuilder()
            .AddText(Bound(title, 200))
            .AddText(Bound(message, 1_000));
        if (!string.IsNullOrWhiteSpace(activationArgument))
        {
            builder.AddArgument("action", Bound(activationArgument, 200));
        }

        AppNotificationManager.Default.Show(builder.BuildNotification());
        return ValueTask.CompletedTask;
    }

    private static string Bound(string value, int length)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Length <= length ? value : value[..length];
    }
}
