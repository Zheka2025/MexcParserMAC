using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace MexcSetupApp.Maui.Platforms.Windows;

public static class WindowExtensions
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;

    public static void SetAlwaysOnTop(this Microsoft.Maui.Controls.Window window, bool alwaysOnTop)
    {
#if WINDOWS
        var nativeWindow = window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (nativeWindow != null)
        {
            var hwnd = WindowNative.GetWindowHandle(nativeWindow);
            if (alwaysOnTop)
            {
                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            }
        }
#endif
    }
}


