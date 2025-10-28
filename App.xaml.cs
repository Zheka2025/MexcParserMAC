namespace MexcSetupApp.Maui;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new AppShell())
		{
			Title = "MEXC Setup & Parser",
			Width = 1024,
			Height = 768
		};
		
		// Максимізуємо вікно при запуску (на весь екран з кнопками)
		window.Created += (s, e) =>
		{
#if WINDOWS
			MaximizeWindowOnStartup(window);
#elif MACCATALYST
			MaximizeWindowOnStartup(window);
#endif
		};
		
		return window;
	}

#if WINDOWS
	private void MaximizeWindowOnStartup(Window window)
	{
		var nativeWindow = window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
		if (nativeWindow != null)
		{
			var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
			var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
			var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
			
			// Maximize вікно
			var presenter = appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
			if (presenter != null)
			{
				presenter.Maximize();
			}
		}
	}
#elif MACCATALYST
	private void MaximizeWindowOnStartup(Window window)
	{
		// На Mac maximize працює автоматично через window sizing
	}
#endif
}