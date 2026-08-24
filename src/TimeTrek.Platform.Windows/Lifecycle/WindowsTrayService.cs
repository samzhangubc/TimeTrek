using System.Runtime.InteropServices;
using TimeTrek.Application.Lifecycle;

namespace TimeTrek.Platform.Windows.Lifecycle;

/// <summary>Owns the process notification-area icon and its HWND subclass.</summary>
public sealed class WindowsTrayService : ITrayService
{
    private const uint CallbackMessage = 0x8001;
    private const uint NotifyAdd = 0;
    private const uint NotifyModify = 1;
    private const uint NotifyDelete = 2;
    private const uint NotifyMessage = 1;
    private const uint NotifyIcon = 2;
    private const uint NotifyTip = 4;
    private const uint ImageIcon = 1;
    private const uint LoadFromFile = 0x10;
    private const uint LoadDefaultSize = 0x40;
    private const uint LeftButtonDoubleClick = 0x0203;
    private const uint LeftButtonUp = 0x0202;
    private const nuint SubclassId = 0x54494D45;

    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly SubclassProcedure subclassProcedure;
    private nint windowHandle;
    private nint iconHandle;
    private bool visible;
    private bool disposed;

    public WindowsTrayService() => subclassProcedure = OnWindowMessage;

    public event EventHandler? RestoreRequested;

    public async ValueTask InitializeAsync(nint windowHandle, string iconPath, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentOutOfRangeException.ThrowIfEqual(windowHandle, nint.Zero);
        ArgumentException.ThrowIfNullOrWhiteSpace(iconPath);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (this.windowHandle != nint.Zero)
            {
                return;
            }

            string fullPath = Path.GetFullPath(iconPath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("The notification-area icon was not found.", fullPath);
            }

            iconHandle = LoadImage(nint.Zero, fullPath, ImageIcon, 0, 0, LoadFromFile | LoadDefaultSize);
            if (iconHandle == nint.Zero)
            {
                throw new InvalidOperationException("Windows could not load the notification-area icon.");
            }

            if (!SetWindowSubclass(windowHandle, subclassProcedure, SubclassId, nint.Zero))
            {
                _ = DestroyIcon(iconHandle);
                iconHandle = nint.Zero;
                throw new InvalidOperationException("Windows could not attach the notification-area callback.");
            }

            this.windowHandle = windowHandle;
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask ShowAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureInitialized();
            if (!visible)
            {
                NOTIFYICONDATA data = CreateData("TimeTrek");
                if (!ShellNotifyIcon(NotifyAdd, ref data))
                {
                    throw new InvalidOperationException("Windows could not create the notification-area icon.");
                }

                visible = true;
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask HideAsync(CancellationToken cancellationToken = default)
    {
        if (disposed)
        {
            return;
        }

        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (visible)
            {
                NOTIFYICONDATA data = CreateData(string.Empty);
                _ = ShellNotifyIcon(NotifyDelete, ref data);
                visible = false;
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask UpdateTimerAsync(TimeSpan elapsed, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (visible)
            {
                string tooltip = $"TimeTrek · {(long)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
                NOTIFYICONDATA data = CreateData(tooltip);
                _ = ShellNotifyIcon(NotifyModify, ref data);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        await HideAsync().ConfigureAwait(false);
        if (windowHandle != nint.Zero)
        {
            _ = RemoveWindowSubclass(windowHandle, subclassProcedure, SubclassId);
            windowHandle = nint.Zero;
        }

        if (iconHandle != nint.Zero)
        {
            _ = DestroyIcon(iconHandle);
            iconHandle = nint.Zero;
        }

        gate.Dispose();
        disposed = true;
    }

    private nint OnWindowMessage(nint hwnd, uint message, nint wParam, nint lParam, nuint id, nint data)
    {
        _ = id;
        _ = data;
        if (message == CallbackMessage && ((uint)lParam == LeftButtonDoubleClick || (uint)lParam == LeftButtonUp))
        {
            RestoreRequested?.Invoke(this, EventArgs.Empty);
        }

        return DefSubclassProc(hwnd, message, wParam, lParam);
    }

    private NOTIFYICONDATA CreateData(string tooltip) => new()
    {
        Size = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
        WindowHandle = windowHandle,
        Id = 1,
        Flags = NotifyMessage | NotifyIcon | NotifyTip,
        CallbackMessage = CallbackMessage,
        IconHandle = iconHandle,
        Tip = tooltip.Length > 127 ? tooltip[..127] : tooltip,
        Info = string.Empty,
        InfoTitle = string.Empty,
    };

    private void EnsureInitialized()
    {
        if (windowHandle == nint.Zero || iconHandle == nint.Zero)
        {
            throw new InvalidOperationException("The notification-area service must be initialized with a window first.");
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public uint Size;
        public nint WindowHandle;
        public uint Id;
        public uint Flags;
        public uint CallbackMessage;
        public nint IconHandle;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Tip;
        public uint State;
        public uint StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Info;
        public uint TimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string InfoTitle;
        public uint InfoFlags;
        public Guid ItemGuid;
        public nint BalloonIconHandle;
    }

    private delegate nint SubclassProcedure(nint hwnd, uint message, nint wParam, nint lParam, nuint id, nint data);

#pragma warning disable SYSLIB1054
    [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconW", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShellNotifyIcon(uint message, ref NOTIFYICONDATA data);

    [DllImport("user32.dll", EntryPoint = "LoadImageW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern nint LoadImage(nint instance, string name, uint type, int desiredWidth, int desiredHeight, uint loadFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(nint icon);

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
