namespace MexcSetupApp.Maui;

public class Config
{
    public int? api_id { get; set; }
    public string? api_hash { get; set; }
    public string? phone_number { get; set; }
    // Legacy single channel (kept for backward compatibility)
    public string? channel { get; set; }
    // New dual-channel support
    public string? listing_channel { get; set; }
    public string? delisting_channel { get; set; }
    public string? session_pathname { get; set; }
    public string? user_data_dir { get; set; }
    public bool mexc_logged { get; set; }

    // Filters
    public FilterSettings filters { get; set; } = new();
}

public class FilterSettings
{
    public bool enabled { get; set; } = false;
    // Legacy single-rule fields (збережено для сумісності)
    public string action { get; set; } = "";
    public string exchanges { get; set; } = "";
    public string market { get; set; } = "";
    // Текстові шаблони (по одному на рядок) з плейсхолдером {TOKEN}
    public string[] pattern_lines { get; set; } = Array.Empty<string>();
    // Окремі шаблони для листингу/делістингу (редагуються вручну у config.json)
    public string[] listing_patterns { get; set; } = new[]
    {
        "{TOKEN} listed on Binance spot",
        "{TOKEN} listed on Binance futures",
        "{TOKEN} listed on Hyperliquid futures",
        "{TOKEN} listed on Upbit spot",
        "{TOKEN} listed on Upbit spot (KRW)",
        "{TOKEN} listed on Bithumb spot",
        "{TOKEN} listed on Coinbase futures"
    };
    public string[] delisting_patterns { get; set; } = new[]
    {
        "{TOKEN} delisted from Bybit futures",
        "{TOKEN} delisted from Binance futures",
        "{TOKEN} delisted from Coinbase futures"
    };
    // Нова модель: список правил (OR) — більше не використовується, залишено для сумісності
    public List<FilterRule> rules { get; set; } = new();
}

public class FilterRule
{
    // "listed", "delisted" або пусто = будь-яка дія
    public string action { get; set; } = "";
    // Кома-розділений список бірж (Upbit,Binance,Bybit ...), пусто = будь-яка
    public string exchanges { get; set; } = "";
    // "spot" або "futures" або пусто = будь-який
    public string market { get; set; } = "";
    public override string ToString()
    {
        var a = string.IsNullOrWhiteSpace(action) ? "(any)" : action;
        var e = string.IsNullOrWhiteSpace(exchanges) ? "(any)" : exchanges;
        var m = string.IsNullOrWhiteSpace(market) ? "(any)" : market;
        return $"action={a}; exchanges={e}; market={m}";
    }
}

