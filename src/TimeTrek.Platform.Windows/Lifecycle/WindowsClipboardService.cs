using TimeTrek.Application.Lifecycle;
using Windows.ApplicationModel.DataTransfer;

namespace TimeTrek.Platform.Windows.Lifecycle;

public sealed class WindowsClipboardService : IClipboardService
{
    public ValueTask SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(text);
        DataPackage package = new();
        package.SetText(text);
        Clipboard.SetContent(package);
        Clipboard.Flush();
        return ValueTask.CompletedTask;
    }
}
