using TimeTrek.Application.Lifecycle;
using Windows.ApplicationModel;

namespace TimeTrek.Platform.Windows.Lifecycle;

public sealed class WindowsStartupRegistrationService : IStartupRegistrationService
{
    private const string TaskId = "TimeTrekStartup";

    public async ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        StartupTask task = await StartupTask.GetAsync(TaskId);
        return task.State == StartupTaskState.Enabled;
    }

    public async ValueTask SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        StartupTask task = await StartupTask.GetAsync(TaskId);
        if (enabled && task.State != StartupTaskState.Enabled)
        {
            StartupTaskState state = await task.RequestEnableAsync();
            if (state != StartupTaskState.Enabled)
            {
                throw new InvalidOperationException("Windows did not enable TimeTrek startup registration.");
            }
        }
        else if (!enabled && task.State == StartupTaskState.Enabled)
        {
            task.Disable();
        }
    }
}
