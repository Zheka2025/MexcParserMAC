using Newtonsoft.Json;
using System.Diagnostics;
using Telegram.Bot;
#if WINDOWS
using MexcSetupApp.Maui.Platforms.Windows;
#elif MACCATALYST
using MexcSetupApp.Maui.Platforms.MacCatalyst;
#endif

namespace MexcSetupApp.Maui;

public partial class MainPage : ContentPage
{
    private Config _cfg = new();
    private TelegramParser? _parser;
    private bool _vipCheckPassed = false;
    private const string ConfigFile = "config.json";

	public MainPage()
	{
		InitializeComponent();
        LoadConfigIfExists();
        
        // Перевірка VIP при запуску
        _ = CheckVipStatusAsync();
    }

    private void LoadConfigIfExists()
    {
        string appDataDir = FileSystem.AppDataDirectory;
        if (!Directory.Exists(appDataDir))
            Directory.CreateDirectory(appDataDir);
            
        var configPath = Path.Combine(appDataDir, ConfigFile);
        if (File.Exists(configPath))
        {
            try
            {
                _cfg = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath)) ?? new Config();
                ApiIdEntry.Text = _cfg.api_id?.ToString() ?? "";
                ApiHashEntry.Text = _cfg.api_hash ?? "";
                PhoneEntry.Text = _cfg.phone_number ?? "";
                BotTokenEntry.Text = _cfg.bot_token ?? "";
                var listing = string.IsNullOrWhiteSpace(_cfg.listing_channel) ? _cfg.channel : _cfg.listing_channel;
                ListingChannelEntry.Text = listing ?? "";
                DelistingChannelEntry.Text = _cfg.delisting_channel ?? "";
                UserDataDirEntry.Text = string.IsNullOrWhiteSpace(_cfg.user_data_dir) ? UserDataDirEntry.Text : _cfg.user_data_dir;
                
                if (_cfg.filters != null)
                {
                    FiltersEnabled.IsToggled = _cfg.filters.enabled;
                    ListingPatternsEditor.Text = _cfg.filters.listing_patterns != null ? string.Join("\n", _cfg.filters.listing_patterns) : "";
                    DelistingPatternsEditor.Text = _cfg.filters.delisting_patterns != null ? string.Join("\n", _cfg.filters.delisting_patterns) : "";
                }
            }
            catch (Exception ex)
            {
                Status($"Config load error: {ex.Message}", true);
            }
        }
    }

    private void OnSaveClicked(object sender, EventArgs e)
    {
        _cfg.api_id = int.TryParse(ApiIdEntry.Text?.Trim(), out var apiId) ? apiId : null;
        _cfg.api_hash = ApiHashEntry.Text?.Trim();
        _cfg.phone_number = PhoneEntry.Text?.Trim();
        _cfg.bot_token = BotTokenEntry.Text?.Trim();
        _cfg.channel = ListingChannelEntry.Text?.Trim();
        _cfg.listing_channel = ListingChannelEntry.Text?.Trim();
        _cfg.delisting_channel = DelistingChannelEntry.Text?.Trim();
        _cfg.user_data_dir = UserDataDirEntry.Text?.Trim();
        _cfg.session_pathname ??= Path.Combine(FileSystem.AppDataDirectory, "user.session");

        // Save filters
        if (_cfg.filters == null) _cfg.filters = new FilterSettings();
        _cfg.filters.enabled = FiltersEnabled.IsToggled;
        _cfg.filters.listing_patterns = ListingPatternsEditor.Text?.Split('\n', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        _cfg.filters.delisting_patterns = DelistingPatternsEditor.Text?.Split('\n', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

        var configPath = Path.Combine(FileSystem.AppDataDirectory, ConfigFile);
        File.WriteAllText(configPath, JsonConvert.SerializeObject(_cfg, Formatting.Indented));
        Status("Saved config.json ✔");
    }

    private void OnToggleVisibilityClicked(object sender, EventArgs e)
    {
        // TODO: Implement
        Status("Toggle visibility not implemented yet");
    }

    private async void OnConnectClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(BotTokenEntry.Text))
        {
            await DisplayAlert("Error", "Bot token required", "OK");
            return;
        }

        _cfg.bot_token = BotTokenEntry.Text.Trim();

        Status("Connecting to Telegram...");

        await Task.Run(async () =>
        {
            try
            {
                if (_cfg.bot_token == null)
                {
                    MainThread.BeginInvokeOnMainThread(() => Status("❌ Bot token is null"));
                    return;
                }
                
                var client = new TelegramBotClient(_cfg.bot_token);
                var me = await client.GetMeAsync();
                
                MainThread.BeginInvokeOnMainThread(() => Status($"✅ Connected as @{me.Username}"));
                MainThread.BeginInvokeOnMainThread(() => StartParserBtn.IsEnabled = true);
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() => Status("❌ Telegram error: " + ex.Message));
            }
        });
    }

    private async void OnStartParserClicked(object sender, EventArgs e)
    {
        if (_parser != null)
        {
            await _parser.StopAsync();
            _parser = null;
            StartParserBtn.Text = "Start Parser";
            Status("Parser stopped");
            return;
        }

        try
        {
            _parser = new TelegramParser(_cfg, Log, OpenToken);
            await _parser.StartAsync();
            StartParserBtn.Text = "Stop Parser";
            Status("Parser started");
        }
        catch (Exception ex)
        {
            Status($"Parser error: {ex.Message}", true);
        }
    }

    private async void OnMexcLoginClicked(object sender, EventArgs e)
    {
        try
        {
            Status("Opening MEXC login...");
            string url = "https://www.mexc.com/futures/BTC_USDT";

            var webViewPage = new MexcWebViewPage(url, (success) =>
            {
                if (success)
                {
                    _cfg.mexc_logged = true;
                    var configPath = Path.Combine(FileSystem.AppDataDirectory, ConfigFile);
                    File.WriteAllText(configPath, JsonConvert.SerializeObject(_cfg, Formatting.Indented));
                    Status("✅ MEXC session saved");
                }
            });
            
            var window = new Window(webViewPage)
            {
                Title = "MEXC Login",
                Width = 1280,
                Height = 850
            };
            
            Application.Current?.OpenWindow(window);
            
            // Робимо вікно поверх всіх (Windows і Mac)
            window.SetAlwaysOnTop(true);
            
            // Максимізуємо вікно браузера
#if WINDOWS
            MaximizeWebViewWindow(window);
#endif
            Status("✅ MEXC WebView opened in new window");
        }
        catch (Exception ex)
        {
            Status("WebView error: " + ex.Message, true);
        }
    }

    private void OnOpenTokenClicked(object sender, EventArgs e)
    {
        var token = TokenEntry.Text?.Trim();
        if (string.IsNullOrEmpty(token))
        {
            Status("Enter token first", true);
            return;
        }

        OpenToken(token, "manual");
    }

    private void OpenToken(string token, string source)
    {
        try
        {
            Status($"Opening ${token} ({source})...");
            string url = $"https://www.mexc.com/futures/{token}_USDT";

            var webViewPage = new MexcWebViewPage(url, (success) =>
            {
                if (success)
                {
                    Status($"✅ ${token} opened successfully");
                }
            });
            
            var window = new Window(webViewPage)
            {
                Title = $"${token} - MEXC",
                Width = 1280,
                Height = 850
            };
            
            Application.Current?.OpenWindow(window);
            
            // Робимо вікно поверх всіх (Windows і Mac)
            window.SetAlwaysOnTop(true);
            
            // Максимізуємо вікно браузера
#if WINDOWS
            MaximizeWebViewWindow(window);
#endif
        }
        catch (Exception ex)
        {
            Status($"Token error: {ex.Message}", true);
        }
    }

    private void OnFiltersClicked(object sender, EventArgs e)
    {
        // TODO: Implement filters window
        Status("Filters not implemented yet");
    }

    private void Log(string message)
    {
        MainThread.BeginInvokeOnMainThread(() => Status(message));
    }

    private void Status(string message, bool isError = false)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text = message;
            StatusLabel.TextColor = isError ? Colors.Red : Colors.Black;
        });
    }

    private async Task CheckVipStatusAsync()
    {
        if (_vipCheckPassed) return;

        string sessionDir = FileSystem.AppDataDirectory;
        if (!Directory.Exists(sessionDir))
            Directory.CreateDirectory(sessionDir);
            
        string sessionPath = _cfg.session_pathname ?? Path.Combine(sessionDir, "user.session");
        if (!File.Exists(sessionPath))
        {
            Log("⚠ Service check skipped: no session file");
            StartParserBtn.IsEnabled = true; // На Mac дозволяємо без VIP check
            return;
        }

        // Показуємо індикатор завантаження
        MainThread.BeginInvokeOnMainThread(() => Status("Checking system requirements..."));

        await Task.Run(async () =>
        {
            try
            {
                // Тимчасово пропускаємо VIP check для macOS
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _vipCheckPassed = true;
                    StartParserBtn.IsEnabled = true;
                    Status("✅ System ready");
                });
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Status($"⚠ VIP check failed: {ex.Message}");
                    StartParserBtn.IsEnabled = true; // Дозволяємо працювати навіть без VIP
                });
            }
        });
    }

#if WINDOWS
    private void MaximizeWebViewWindow(Microsoft.Maui.Controls.Window window)
    {
        // Затримка для ініціалізації вікна
        Task.Delay(100).ContinueWith(_ =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var nativeWindow = window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                if (nativeWindow != null)
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
                    var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                    var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
                    
                    var presenter = appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
                    if (presenter != null)
                    {
                        presenter.Maximize();
                    }
                }
            });
        });
    }
#endif
}