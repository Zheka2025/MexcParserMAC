using Foundation;
using UIKit;

namespace MexcSetupApp.Maui.Platforms.MacCatalyst;

public static class WindowExtensions
{
    public static void SetAlwaysOnTop(this Microsoft.Maui.Controls.Window window, bool alwaysOnTop)
    {
#if MACCATALYST
        // На Mac можна використати UIWindow level для "поверх всіх"
        var uiWindow = window?.Handler?.PlatformView as UIWindow;
        if (uiWindow != null && alwaysOnTop)
        {
            uiWindow.WindowLevel = UIWindowLevel.StatusBar;
        }
#endif
    }
}


