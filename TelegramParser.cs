using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TdLib;

namespace MexcSetupApp.Maui
{
    public class TelegramParser
    {
        private readonly Config _cfg;
        private readonly Action<string> _log;
        private readonly Action<string, string> _openToken;
        private TdClient? _client;
        private CancellationTokenSource? _cts;
        private bool _isRunning = false;
        private readonly Regex _rx = new(@"\$(?<t>[A-Za-z0-9_]{2,20})", RegexOptions.Compiled);
        private readonly Regex _rxDetailed = new(@"\$(?<t>[A-Za-z0-9_]{2,20})(?:\s*,\s*\$(?<t2>[A-Za-z0-9_]{2,20}))?\s+(?<action>listed|delisted)\s+(?:on|from)\s+(?<exchange>[A-Za-z0-9_.-]+)\s+(?<market>spot|futures?|perp(?:etual)?)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private long? _listingChannelId;
        private long? _delistingChannelId;

        public TelegramParser(Config cfg, Action<string> log, Action<string, string> openToken)
        {
            _cfg = cfg;
            _log = log;
            _openToken = openToken;
        }

        public async Task StartAsync()
        {
            if (_isRunning) return;
            _isRunning = true;
            _cts = new CancellationTokenSource();

            _log("Parser starting...");
            
            _log("=== CONFIG DEBUG ===");
            _log($"filters != null: {_cfg.filters != null}");
            if (_cfg.filters != null)
            {
                _log($"filters.enabled: {_cfg.filters.enabled}");
                _log($"listing_patterns count: {_cfg.filters.listing_patterns?.Length ?? 0}");
                if (_cfg.filters.listing_patterns != null)
                {
                    for (int i = 0; i < _cfg.filters.listing_patterns.Length; i++)
                        _log($"  listing_patterns[{i}]: '{_cfg.filters.listing_patterns[i]}'");
                }
                _log($"delisting_patterns count: {_cfg.filters.delisting_patterns?.Length ?? 0}");
                if (_cfg.filters.delisting_patterns != null)
                {
                    for (int i = 0; i < _cfg.filters.delisting_patterns.Length; i++)
                        _log($"  delisting_patterns[{i}]: '{_cfg.filters.delisting_patterns[i]}'");
                }
            }
            _log("=== END CONFIG DEBUG ===");

            try
            {
                if (_cfg.api_id == null || string.IsNullOrEmpty(_cfg.api_hash))
                {
                    _log("❌ API ID and API Hash required");
                    return;
                }

                _client = new TdClient();
                
                // Set TDLib parameters
                var parameters = new TdApi.SetTdlibParameters
                {
                    ApiId = _cfg.api_id.Value,
                    ApiHash = _cfg.api_hash,
                    DatabaseDirectory = Path.Combine(FileSystem.AppDataDirectory, "tdlib"),
                    FilesDirectory = Path.Combine(FileSystem.AppDataDirectory, "tdlib_files"),
                    UseFileDatabase = true,
                    UseChatInfoDatabase = true,
                    UseMessageDatabase = true,
                    UseSecretChats = false,
                    SystemLanguageCode = "en",
                    DeviceModel = "Desktop",
                    SystemVersion = "1.0",
                    ApplicationVersion = "1.0"
                };

                await _client.ExecuteAsync(parameters);
                
                _log("✅ TDLib initialized");
                
                // Start receiving updates
                _client.UpdateReceived += OnUpdateReceived;
                
                _log("✅ Parser started successfully");
            }
            catch (Exception ex)
            {
                _log($"❌ Parser error: {ex.Message}");
                _isRunning = false;
            }
        }

        public Task StopAsync()
        {
            if (!_isRunning) return Task.CompletedTask;
            
            _log("Stopping parser...");
            _cts?.Cancel();
            _client?.Dispose();
            _isRunning = false;
            _log("✅ Parser stopped");
            return Task.CompletedTask;
        }

        private void OnUpdateReceived(object? sender, TdApi.Update update)
        {
            try
            {
                if (update is TdApi.Update.UpdateNewMessage newMessage)
                {
                    var message = newMessage.Message;
                    if (message.Content is TdApi.MessageContent.MessageText textContent)
                    {
                        var text = textContent.Text.Text;
                        var chatId = message.ChatId;

                        _log($"📨 Message from {chatId}: {text}");

                        // Check if this is a listing/delisting channel
                        if (_listingChannelId == null || _delistingChannelId == null)
                        {
                            if (text.Contains("listed") || text.Contains("delisted"))
                            {
                                if (_listingChannelId == null && text.Contains("listed"))
                                {
                                    _listingChannelId = chatId;
                                    _log($"📌 Set listing channel: {chatId}");
                                }
                                if (_delistingChannelId == null && text.Contains("delisted"))
                                {
                                    _delistingChannelId = chatId;
                                    _log($"📌 Set delisting channel: {chatId}");
                                }
                            }
                        }

                        // Process message with filters
                        ProcessMessage(text, chatId);
                    }
                }
            }
            catch (Exception ex)
            {
                _log($"❌ Update error: {ex.Message}");
            }
        }

        private void ProcessMessage(string message, long chatId)
        {
            if (_cfg.filters?.enabled != true) return;

            try
            {
                // Check listing patterns
                if (_cfg.filters.listing_patterns != null)
                {
                    foreach (var pattern in _cfg.filters.listing_patterns)
                    {
                        if (string.IsNullOrEmpty(pattern)) continue;
                        
                        if (message.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                        {
                            var matches = _rx.Matches(message);
                            foreach (Match match in matches)
                            {
                                var token = match.Groups["t"].Value;
                                _log($"🎯 Listing match: ${token} (pattern: {pattern})");
                                _openToken(token, "listing");
                            }
                        }
                    }
                }

                // Check delisting patterns
                if (_cfg.filters.delisting_patterns != null)
                {
                    foreach (var pattern in _cfg.filters.delisting_patterns)
                    {
                        if (string.IsNullOrEmpty(pattern)) continue;
                        
                        if (message.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                        {
                            var matches = _rx.Matches(message);
                            foreach (Match match in matches)
                            {
                                var token = match.Groups["t"].Value;
                                _log($"🎯 Delisting match: ${token} (pattern: {pattern})");
                                _openToken(token, "delisting");
                            }
                        }
                    }
                }

                // Detailed pattern matching
                var detailedMatches = _rxDetailed.Matches(message);
                foreach (Match match in detailedMatches)
                {
                    var token1 = match.Groups["t"].Value;
                    var token2 = match.Groups["t2"].Value;
                    var action = match.Groups["action"].Value;
                    var exchange = match.Groups["exchange"].Value;
                    var market = match.Groups["market"].Value;

                    _log($"🎯 Detailed match: ${token1} {action} on {exchange} ({market})");
                    
                    if (!string.IsNullOrEmpty(token1))
                        _openToken(token1, action);
                    if (!string.IsNullOrEmpty(token2))
                        _openToken(token2, action);
                }
            }
            catch (Exception ex)
            {
                _log($"❌ Process message error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _client?.Dispose();
            _cts?.Dispose();
        }
    }
}