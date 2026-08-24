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
    private nint dynamicIconHandle;
    private string? dynamicIconText;
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
                string compact = $"{Math.Min(99, (long)elapsed.TotalHours)}:{elapsed.Minutes:00}";
                nint previousDynamicIcon = dynamicIconHandle;
                string? previousDynamicText = dynamicIconText;
                bool iconChanged = false;
                if (!string.Equals(compact, dynamicIconText, StringComparison.Ordinal))
                {
                    nint candidate = TryCreateTimerIcon(compact);
                    if (candidate != nint.Zero)
                    {
                        dynamicIconHandle = candidate;
                        dynamicIconText = compact;
                        iconChanged = true;
                    }
                }
                NOTIFYICONDATA data = CreateData(tooltip);
                bool modified = ShellNotifyIcon(NotifyModify, ref data);
                if (modified && iconChanged && previousDynamicIcon != nint.Zero)
                {
                    _ = DestroyIcon(previousDynamicIcon);
                }
                else if (!modified && iconChanged)
                {
                    _ = DestroyIcon(dynamicIconHandle);
                    dynamicIconHandle = previousDynamicIcon;
                    dynamicIconText = previousDynamicText;
                }
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

        if (dynamicIconHandle != nint.Zero)
        {
            _ = DestroyIcon(dynamicIconHandle);
            dynamicIconHandle = nint.Zero;
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
        IconHandle = dynamicIconHandle != nint.Zero ? dynamicIconHandle : iconHandle,
        Tip = tooltip.Length > 127 ? tooltip[..127] : tooltip,
        Info = string.Empty,
        InfoTitle = string.Empty,
    };

    private static nint CreateTimerIcon(string text)
    {
        nint screen = GetDC(nint.Zero);
        if (screen == nint.Zero)
        {
            throw new InvalidOperationException("Windows could not create the timer icon drawing surface.");
        }

        nint drawing = CreateCompatibleDC(screen);
        nint color = CreateCompatibleBitmap(screen, 32, 32);
        nint mask = CreateBitmap(32, 32, 1, 1, nint.Zero);
        nint font = CreateFont(-12, 0, 0, 0, 700, 0, 0, 0, 1, 0, 0, 5, 0, "Segoe UI");
        nint oldBitmap = nint.Zero;
        nint oldFont = nint.Zero;
        try
        {
            if (drawing == nint.Zero || color == nint.Zero || mask == nint.Zero || font == nint.Zero)
            {
                throw new InvalidOperationException("Windows could not allocate the timer icon.");
            }

            oldBitmap = SelectObject(drawing, color);
            oldFont = SelectObject(drawing, font);
            RECT bounds = new() { Left = 0, Top = 0, Right = 32, Bottom = 32 };
            nint brush = CreateSolidBrush(0x00F7812F);
            try
            {
                _ = FillRect(drawing, ref bounds, brush);
            }
            finally
            {
                _ = DeleteObject(brush);
            }

            _ = SetBkMode(drawing, 1);
            _ = SetTextColor(drawing, 0x00FFFFFF);
            _ = DrawText(drawing, text, text.Length, ref bounds, 0x00000001 | 0x00000004 | 0x00000020);
            ICONINFO info = new() { IsIcon = true, ColorBitmap = color, MaskBitmap = mask };
            nint result = CreateIconIndirect(ref info);
            if (result == nint.Zero)
            {
                throw new InvalidOperationException("Windows could not create the timer icon.");
            }

            return result;
        }
        finally
        {
            if (oldFont != nint.Zero) _ = SelectObject(drawing, oldFont);
            if (oldBitmap != nint.Zero) _ = SelectObject(drawing, oldBitmap);
            if (font != nint.Zero) _ = DeleteObject(font);
            if (color != nint.Zero) _ = DeleteObject(color);
            if (mask != nint.Zero) _ = DeleteObject(mask);
            if (drawing != nint.Zero) _ = DeleteDC(drawing);
            _ = ReleaseDC(nint.Zero, screen);
        }
    }

    private static nint TryCreateTimerIcon(string text)
    {
        try
        {
            return CreateTimerIcon(text);
        }
        catch (InvalidOperationException)
        {
            return nint.Zero;
        }
    }

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

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ICONINFO
    {
        [MarshalAs(UnmanagedType.Bool)] public bool IsIcon;
        public uint XHotspot;
        public uint YHotspot;
        public nint MaskBitmap;
        public nint ColorBitmap;
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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint GetDC(nint window);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(nint window, nint deviceContext);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern nint CreateCompatibleDC(nint deviceContext);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern nint CreateCompatibleBitmap(nint deviceContext, int width, int height);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern nint CreateBitmap(int width, int height, uint planes, uint bitsPerPixel, nint bits);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern nint SelectObject(nint deviceContext, nint value);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern nint CreateSolidBrush(uint color);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int FillRect(nint deviceContext, ref RECT rectangle, nint brush);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateFont(int height, int width, int escapement, int orientation, int weight,
        uint italic, uint underline, uint strikeOut, uint characterSet, uint outputPrecision, uint clipPrecision,
        uint quality, uint pitchAndFamily, string faceName);

    [DllImport("gdi32.dll")]
    private static extern int SetBkMode(nint deviceContext, int mode);

    [DllImport("gdi32.dll")]
    private static extern uint SetTextColor(nint deviceContext, uint color);

    [DllImport("user32.dll", EntryPoint = "DrawTextW", CharSet = CharSet.Unicode)]
    private static extern int DrawText(nint deviceContext, string text, int textLength, ref RECT rectangle, uint format);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint CreateIconIndirect(ref ICONINFO iconInfo);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(nint value);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteDC(nint deviceContext);
#pragma warning restore SYSLIB1054
}
