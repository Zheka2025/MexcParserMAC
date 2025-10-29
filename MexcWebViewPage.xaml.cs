namespace MexcSetupApp.Maui;

public partial class MexcWebViewPage : ContentPage
{
    private readonly Action<bool>? _onCookiesSaved;

    public MexcWebViewPage(string url, Action<bool>? onCookiesSaved = null)
    {
        InitializeComponent();
        _onCookiesSaved = onCookiesSaved;
        
        MexcWebView.Source = url;
    }

    private void OnNavigated(object? sender, WebNavigatedEventArgs e)
    {
        // Коли завантажився MEXC, вважаємо що cookies збережені
        if (e.Result == WebNavigationResult.Success)
        {
            _onCookiesSaved?.Invoke(true);
        }
    }
}


