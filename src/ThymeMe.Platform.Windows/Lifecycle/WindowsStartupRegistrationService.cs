using System.Runtime.InteropServices;
using Microsoft.Win32;
using ThymeMe.Application.Lifecycle;
using Windows.ApplicationModel;

namespace ThymeMe.Platform.Windows.Lifecycle;

public sealed class WindowsStartupRegistrationService : IStartupRegistrationService
{
    private const string TaskId = "thymeme.startup";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "thymeme";

    public async ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsPackaged())
        {
            StartupTask task = await StartupTask.GetAsync(TaskId);
            return task.State == StartupTaskState.Enabled;
        }

        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        string? command = key?.GetValue(RunValueName) as string;
        return string.Equals(command, Quote(GetUserEntryExecutablePath()), StringComparison.OrdinalIgnoreCase);
    }

    public async ValueTask SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsPackaged())
        {
            StartupTask task = await StartupTask.GetAsync(TaskId);
            if (enabled && task.State != StartupTaskState.Enabled)
            {
                StartupTaskState state = await task.RequestEnableAsync();
                if (state != StartupTaskState.Enabled)
                {
                    throw new InvalidOperationException("Windows did not enable Thyme-Me startup registration.");
                }
            }
            else if (!enabled && task.State == StartupTaskState.Enabled)
            {
                task.Disable();
            }

            return;
        }

        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Windows did not provide access to per-user startup settings.");
        if (enabled)
        {
            key.SetValue(RunValueName, Quote(GetUserEntryExecutablePath()), RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(RunValueName, throwOnMissingValue: false);
        }
    }

    internal static string GetUserEntryExecutablePath()
    {
        string executable = Environment.ProcessPath
            ?? throw new InvalidOperationException("Windows did not provide the Thyme-Me executable path.");
        string directory = Path.GetDirectoryName(executable)!;
        if (string.Equals(Path.GetFileName(directory), "app", StringComparison.OrdinalIgnoreCase))
        {
            string filesDirectory = Path.GetDirectoryName(directory)!;
            string launcher = Path.Combine(Path.GetDirectoryName(filesDirectory)!, "thymeme.exe");
            if (File.Exists(launcher))
            {
                return launcher;
            }
        }

        return executable;
    }

    private static string Quote(string path) => $"\"{path}\"";

    private static bool IsPackaged()
    {
        try
        {
            _ = Package.Current.Id;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (COMException exception) when ((uint)exception.HResult == 0x80073D54)
        {
            return false;
        }
    }
}
