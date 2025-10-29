using Newtonsoft.Json;
using System.Diagnostics;
using TL;
using WTelegram;
#if WINDOWS
using MexcSetupApp.Maui.Platforms.Windows;
#elif MACCATALYST
using MexcSetupApp.Maui.Platforms.MacCatalyst;
#endif

namespace MexcSetupApp.Maui;

public partial class MainPage : ContentPage
{
    private const string ConfigFile = "config.json";
    private Config _cfg = new();

    private TelegramParser? _parser;
    private bool _parserRunning = false;

    private bool _fieldsHidden = false;
    private string _realApiId = "";
    private string _realApiHash = "";
    private string _realPhone = "";
    private string _realListingChannel = "";
    private string _realDelistingChannel = "";
    private string _realUserDataDir = "";

    // VIP перевірка
    private const long VIP_CHANNEL_ID = 2489315134; // ID без префіксу -100
    private bool _vipCheckPassed = false;

	public MainPage()
	{
		InitializeComponent();
        
        string appDir = Path.Combine(FileSystem.AppDataDirectory, "MexcOpener");
        if (!Directory.Exists(appDir)) Directory.CreateDirectory(appDir);

        string defaultWebDir = Path.Combine(appDir, "WebViewData");
        if (!Directory.Exists(defaultWebDir)) Directory.CreateDirectory(defaultWebDir);

        UserDataDirEntry.Text = defaultWebDir;
        StartParserBtn.IsEnabled = false; // Блокуємо до перевірки VIP
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
                var listing = string.IsNullOrWhiteSpace(_cfg.listing_channel) ? _cfg.channel : _cfg.listing_channel;
                ListingChannelEntry.Text = listing ?? "";
                DelistingChannelEntry.Text = _cfg.delisting_channel ?? "";
                UserDataDirEntry.Text = string.IsNullOrWhiteSpace(_cfg.user_data_dir) ? UserDataDirEntry.Text : _cfg.user_data_dir;
                
                var sessionPath = string.IsNullOrWhiteSpace(_cfg.session_pathname) 
                    ? Path.Combine(FileSystem.AppDataDirectory, "user.session")
                    : _cfg.session_pathname;
                    
                if (File.Exists(sessionPath))
                    Status("Found Telegram session ✔");
                if (_cfg.mexc_logged) MexcLoginBtn.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Status("Config read error: " + ex.Message, true);
            }
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (!int.TryParse(ApiIdEntry.Text?.Trim(), out var apiId))
        {
            await DisplayAlert("Error", "api_id має бути числом", "OK");
            return;
        }

        _cfg.api_id = apiId;
        _cfg.api_hash = ApiHashEntry.Text?.Trim();
        _cfg.phone_number = PhoneEntry.Text?.Trim();
        _cfg.listing_channel = ListingChannelEntry.Text?.Trim();
        _cfg.delisting_channel = DelistingChannelEntry.Text?.Trim();
        _cfg.channel = _cfg.listing_channel;
        _cfg.user_data_dir = UserDataDirEntry.Text?.Trim();
        _cfg.session_pathname ??= Path.Combine(FileSystem.AppDataDirectory, "user.session");

        var configPath = Path.Combine(FileSystem.AppDataDirectory, ConfigFile);
        File.WriteAllText(configPath, JsonConvert.SerializeObject(_cfg, Formatting.Indented));
        Status("Saved config.json ✔");
    }

    private async void OnConnectClicked(object sender, EventArgs e)
    {
#if MACCATALYST
        // На macOS WTelegramClient не підтримується
        await DisplayAlert("macOS Notice", 
            "Telegram connection currently works only on Windows.\n\nOn Mac you can:\n- Use MEXC login and manual token entry\n- Or run parser on Windows machine", 
            "OK");
        Status("⚠️ Telegram not supported on macOS");
        return;
#else
        if (!int.TryParse(ApiIdEntry.Text?.Trim(), out var apiId) ||
            string.IsNullOrWhiteSpace(ApiHashEntry.Text) ||
            string.IsNullOrWhiteSpace(PhoneEntry.Text))
        {
            await DisplayAlert("Error", "Заповни api_id, api_hash і телефон", "OK");
            return;
        }

        _cfg.api_id = apiId;
        _cfg.api_hash = ApiHashEntry.Text?.Trim();
        _cfg.phone_number = PhoneEntry.Text?.Trim();

        string sessionDir = FileSystem.AppDataDirectory;
        if (!Directory.Exists(sessionDir))
            Directory.CreateDirectory(sessionDir);
            
        string sessionPath = Path.Combine(sessionDir, "user.session");
        _cfg.session_pathname = sessionPath;

        Status("Connecting to Telegram...");

        await Task.Run(async () =>
        {
            Client? client = null;
            try
            {
                client = new Client(What => What switch
                {
                    "api_id" => _cfg.api_id?.ToString(),
                    "api_hash" => _cfg.api_hash,
                    "phone_number" => _cfg.phone_number,
                    "session_pathname" => _cfg.session_pathname,
                    "verification_code" => MainThread.InvokeOnMainThreadAsync(() => Prompt("Введи код із Telegram")).Result,
                    "password" => MainThread.InvokeOnMainThreadAsync(() => Prompt("Введи 2FA пароль (якщо є)", true)).Result,
                    _ => null
                });

                await client.LoginUserIfNeeded();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Status("Telegram connected ✔");
                    MexcLoginBtn.IsEnabled = true;
                    var configPath = Path.Combine(FileSystem.AppDataDirectory, ConfigFile);
                    File.WriteAllText(configPath, JsonConvert.SerializeObject(_cfg, Formatting.Indented));
                });
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() => Status("Telegram error: " + ex.Message, true));
            }
            finally
            {
                try { client?.Dispose(); } catch { }
            }
        });
#endif
    }

    private async Task CheckVipStatusAsync()
    {
#if MACCATALYST
        // На macOS WTelegramClient не працює - пропускаємо VIP check
        _vipCheckPassed = true;
        StartParserBtn.IsEnabled = false; // Parser теж не працюватиме без Telegram
        MexcLoginBtn.IsEnabled = true; // Але MEXC WebView працює!
        Status("macOS mode: Telegram disabled, MEXC WebView available ✔");
        return;
#else
        if (_vipCheckPassed) return;

        string sessionDir = FileSystem.AppDataDirectory;
        if (!Directory.Exists(sessionDir))
            Directory.CreateDirectory(sessionDir);
            
        string sessionPath = _cfg.session_pathname ?? Path.Combine(sessionDir, "user.session");
        if (!File.Exists(sessionPath))
        {
            Log("⚠ Service check skipped: no session file");
            StartParserBtn.IsEnabled = true;
            return;
        }

        MainThread.BeginInvokeOnMainThread(() => Status("Checking system requirements..."));

        await Task.Run(async () =>
        {
            Client? client = null;
            try
            {
                client = new Client(What => What switch
                {
                    "api_id" => _cfg.api_id?.ToString(),
                    "api_hash" => _cfg.api_hash,
                    "phone_number" => _cfg.phone_number,
                    "session_pathname" => sessionPath,
                    _ => null
                });

                await client.LoginUserIfNeeded();
                
                try
                {
                    bool isMember = false;
                    var chats = await client.Messages_GetAllChats();
                    
                    foreach (var kvp in chats.chats)
                    {
                        if (kvp.Value.ID == VIP_CHANNEL_ID || kvp.Key == VIP_CHANNEL_ID)
                        {
                            isMember = true;
                            break;
                        }
                    }

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (isMember)
                        {
                            _vipCheckPassed = true;
                            StartParserBtn.IsEnabled = true;
                            Status("System ready ✔");
                        }
                        else
                        {
                            StartParserBtn.IsEnabled = false;
                            Status("Connection validation failed", true);
                            DisplayAlert("System Error", 
                                "System Security Exception\n\nError Code: 0xC0000142\n\nDescription: The application failed to initialize correctly due to restricted system permissions.\nPlease contact the administrator or reinstall the program.", 
                                "OK");
                        }
                    });
                }
                catch (Exception)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        StartParserBtn.IsEnabled = false;
                        Status("Validation error", true);
                        DisplayAlert("System Error", 
                            "System Security Exception\n\nError Code: 0xC0000142\n\nDescription: The application failed to initialize correctly due to restricted system permissions.\nPlease contact the administrator or reinstall the program.", 
                            "OK");
                    });
                }
            }
            catch (Exception)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StartParserBtn.IsEnabled = false;
                    Status("Session error", true);
                    DisplayAlert("System Error", 
                        "System Security Exception\n\nError Code: 0xC0000142\n\nDescription: The application failed to initialize correctly due to restricted system permissions.\nPlease contact the administrator or reinstall the program.", 
                        "OK");
                });
            }
            finally
            {
                try { client?.Dispose(); } catch { }
            }
        });
#endif
    }

    private void OnMexcLoginClicked(object sender, EventArgs e)
    {
        try
        {
            Status("Opening MEXC login...");
            string url = "https://www.mexc.com/futures/BTC_USDT";

            // MAUI: відкриваємо вбудований WebView в НОВОМУ ВІКНІ
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

    private void Log(string text)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LogsEditor.Text += text + Environment.NewLine;
        });
    }

    private async void OnStartParserClicked(object sender, EventArgs e)
    {
        if (_parserRunning) return;

        _parser = new TelegramParser(_cfg, Log, OpenMexcForToken);
        try
        {
            await _parser.StartAsync();
            _parserRunning = true;
            StartParserBtn.IsEnabled = false;
            StopParserBtn.IsEnabled = true;
            Status("Parser started ✔");
        }
        catch (Exception ex)
        {
            Log("Start error: " + ex.Message);
            Status("Parser start error", true);
        }
    }

    private void OnStopParserClicked(object sender, EventArgs e)
    {
        if (!_parserRunning || _parser == null) return;

        _parser.Stop();
        _parserRunning = false;
        StartParserBtn.IsEnabled = true;
        StopParserBtn.IsEnabled = false;
        Status("Parser stopped");
    }

    private void OpenMexcForToken(string token, string source)
    {
        var url = $"https://www.mexc.com/ru-RU/futures/{token}_USDT?type=linear_swap";
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                var webViewPage = new MexcWebViewPage(url);
                
                var window = new Window(webViewPage)
                {
                    Title = $"MEXC Futures — {token}",
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
                Log($"Opened MEXC for ${token} [{source}]: {url}");
            }
            catch (Exception ex)
            {
                Log("OpenMEXC error: " + ex.Message);
            }
        });
    }

    private async Task<string> Prompt(string title, bool isPassword = false)
    {
        var result = await DisplayPromptAsync(title, "", accept: "OK", cancel: "Cancel", 
            keyboard: isPassword ? Keyboard.Default : Keyboard.Default,
            maxLength: 100);
        return result ?? "";
    }

    private void Status(string text, bool isError = false)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text = "Status: " + text;
            StatusLabel.TextColor = isError ? Colors.Red : Color.FromArgb("#0078D4");
        });
    }

    private string Mask(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return new string('*', Math.Min(value.Length, 8));
    }

    private void OnToggleVisibilityClicked(object sender, EventArgs e)
    {
        if (!_fieldsHidden)
        {
            _realApiId = ApiIdEntry.Text ?? "";
            _realApiHash = ApiHashEntry.Text ?? "";
            _realPhone = PhoneEntry.Text ?? "";
            _realListingChannel = ListingChannelEntry.Text ?? "";
            _realDelistingChannel = DelistingChannelEntry.Text ?? "";
            _realUserDataDir = UserDataDirEntry.Text ?? "";

            ApiIdEntry.Text = Mask(_realApiId);
            ApiHashEntry.Text = Mask(_realApiHash);
            PhoneEntry.Text = Mask(_realPhone);
            ListingChannelEntry.Text = Mask(_realListingChannel);
            DelistingChannelEntry.Text = Mask(_realDelistingChannel);
            UserDataDirEntry.Text = Mask(_realUserDataDir);

            _fieldsHidden = true;
            ToggleVisibilityBtn.Text = "👁 Show";
        }
        else
        {
            ApiIdEntry.Text = _realApiId;
            ApiHashEntry.Text = _realApiHash;
            PhoneEntry.Text = _realPhone;
            ListingChannelEntry.Text = _realListingChannel;
            DelistingChannelEntry.Text = _realDelistingChannel;
            UserDataDirEntry.Text = _realUserDataDir;

            _fieldsHidden = false;
            ToggleVisibilityBtn.Text = "👁 Hide";
        }
    }

#if WINDOWS
    private void MaximizeWebViewWindow(Microsoft.Maui.Controls.Window window)
    {
        // Затримка для ініціалізації вікна
        Task.Delay(200).ContinueWith(_ =>
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
