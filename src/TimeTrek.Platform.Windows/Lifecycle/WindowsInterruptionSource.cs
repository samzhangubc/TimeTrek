using System.Runtime.InteropServices;
using TimeTrek.Application.Lifecycle;
using TimeTrek.Application.Settings;

namespace TimeTrek.Platform.Windows.Lifecycle;

public sealed partial class WindowsInterruptionSource : IInterruptionSource
{
    private const uint PowerBroadcastMessage = 0x0218;
    private const uint SessionChangeMessage = 0x02B1;
    private const uint PowerSettingChange = 0x8013;
    private const uint Suspend = 0x0004;
    private const uint SessionLock = 0x0007;
    private const uint GetScreenSaverRunning = 0x0072;
    private const nuint SubclassId = 0x49525255;
    private static readonly Guid ConsoleDisplayState = new(0x6fe69556, 0x704a, 0x47a0, 0x8f, 0x24, 0xc2, 0x8d, 0x93, 0x6f, 0xda, 0x47);

    private CancellationTokenSource? cancellation;
    private Task? pollingTask;
    private bool idleRaised;
    private bool screenSaverRaised;
    private nint windowHandle;
    private nint powerNotificationHandle;
    private readonly SubclassProcedure subclassProcedure;
    private readonly IAppSettingsStore settingsStore;

    public WindowsInterruptionSource(IAppSettingsStore settingsStore)
    {
        this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        subclassProcedure = OnWindowMessage;
    }

    public event EventHandler<InterruptionEvent>? Interrupted;

    public ValueTask InitializeAsync(nint windowHandle, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentOutOfRangeException.ThrowIfEqual(windowHandle, nint.Zero);
        if (this.windowHandle != nint.Zero)
        {
            return ValueTask.CompletedTask;
        }

        if (!SetWindowSubclass(windowHandle, subclassProcedure, SubclassId, nint.Zero))
        {
            throw new InvalidOperationException("Windows interruption monitoring could not attach to the app window.");
        }

        Guid setting = ConsoleDisplayState;
        powerNotificationHandle = RegisterPowerSettingNotification(windowHandle, ref setting, 0);
        if (powerNotificationHandle == nint.Zero || !WtsRegisterSessionNotification(windowHandle, 0))
        {
            _ = RemoveWindowSubclass(windowHandle, subclassProcedure, SubclassId);
            throw new InvalidOperationException("Windows interruption notifications could not be registered.");
        }

        this.windowHandle = windowHandle;
        return ValueTask.CompletedTask;
    }

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

    public async ValueTask DisposeAsync()
    {
        if (cancellation is not null)
        {
            await cancellation.CancelAsync().ConfigureAwait(false);
            if (pollingTask is not null)
            {
                try
                {
                    await pollingTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }

            cancellation.Dispose();
            cancellation = null;
            pollingTask = null;
        }
        if (windowHandle != nint.Zero)
        {
            _ = WtsUnregisterSessionNotification(windowHandle);
            _ = RemoveWindowSubclass(windowHandle, subclassProcedure, SubclassId);
            windowHandle = nint.Zero;
        }

        if (powerNotificationHandle != nint.Zero)
        {
            _ = UnregisterPowerSettingNotification(powerNotificationHandle);
            powerNotificationHandle = nint.Zero;
        }
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            AppSettings settings = await settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
            if (!settings.InterruptionHandlingEnabled || !settings.StopOnGenericInactivity)
            {
                idleRaised = false;
                continue;
            }

            LASTINPUTINFO info = new() { Size = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
            if (!GetLastInputInfo(ref info))
            {
                continue;
            }

            uint elapsed = unchecked((uint)Environment.TickCount - info.Time);
            bool idle = elapsed >= settings.InactivityThresholdMinutes * 60_000u;
            if (idle && !idleRaised)
            {
                idleRaised = true;
                Interrupted?.Invoke(this, new InterruptionEvent(
                    InterruptionKind.Idle,
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
            }
            else if (!idle)
            {
                idleRaised = false;
            }

            if (settings.InterruptionHandlingEnabled && settings.StopOnScreenSaver &&
                SystemParametersInfo(GetScreenSaverRunning, 0, out bool screenSaverRunning, 0))
            {
                if (screenSaverRunning && !screenSaverRaised)
                {
                    screenSaverRaised = true;
                    Raise(InterruptionKind.ScreenSaver);
                }
                else if (!screenSaverRunning)
                {
                    screenSaverRaised = false;
                }
            }
        }
    }

    private nint OnWindowMessage(nint hwnd, uint message, nint wParam, nint lParam, nuint id, nint data)
    {
        _ = id;
        _ = data;
        if (message == SessionChangeMessage && (uint)wParam == SessionLock)
        {
            Raise(InterruptionKind.Locked);
        }
        else if (message == PowerBroadcastMessage && (uint)wParam == Suspend)
        {
            Raise(InterruptionKind.Suspended);
        }
        else if (message == PowerBroadcastMessage && (uint)wParam == PowerSettingChange && lParam != nint.Zero)
        {
            POWERBROADCASTSETTING setting = Marshal.PtrToStructure<POWERBROADCASTSETTING>(lParam);
            if (setting.PowerSetting == ConsoleDisplayState && setting.DataLength >= sizeof(uint) && Marshal.ReadInt32(lParam, Marshal.SizeOf<POWERBROADCASTSETTING>()) == 0)
            {
                Raise(InterruptionKind.DisplayOff);
            }
        }

        return DefSubclassProc(hwnd, message, wParam, lParam);
    }

    private void Raise(InterruptionKind kind) => Interrupted?.Invoke(
        this,
        new InterruptionEvent(kind, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetLastInputInfo(ref LASTINPUTINFO lastInputInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint Size;
        public uint Time;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POWERBROADCASTSETTING
    {
        public Guid PowerSetting;
        public uint DataLength;
    }

    private delegate nint SubclassProcedure(nint hwnd, uint message, nint wParam, nint lParam, nuint id, nint data);

#pragma warning disable SYSLIB1054
    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(uint action, uint parameter, [MarshalAs(UnmanagedType.Bool)] out bool result, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint RegisterPowerSettingNotification(nint recipient, ref Guid setting, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterPowerSettingNotification(nint handle);

    [DllImport("wtsapi32.dll", EntryPoint = "WTSRegisterSessionNotification", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WtsRegisterSessionNotification(nint window, uint flags);

    [DllImport("wtsapi32.dll", EntryPoint = "WTSUnRegisterSessionNotification", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WtsUnregisterSessionNotification(nint window);

    [DllImport("comctl32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowSubclass(nint hwnd, SubclassProcedure procedure, nuint id, nint data);

    [DllImport("comctl32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveWindowSubclass(nint hwnd, SubclassProcedure procedure, nuint id);

    [DllImport("comctl32.dll")]
    private static extern nint DefSubclassProc(nint hwnd, uint message, nint wParam, nint lParam);
#pragma warning restore SYSLIB1054
}
