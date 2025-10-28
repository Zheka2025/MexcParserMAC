using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WTelegram;
using TL;

namespace MexcSetupApp.Maui
{
    public class TelegramParser
    {
        private readonly Config _cfg;
        private readonly Action<string> _log;
        private readonly Action<string, string> _openToken;
        private Client? _client;
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
            _log("===================");

            var listing = string.IsNullOrWhiteSpace(_cfg.listing_channel) ? _cfg.channel : _cfg.listing_channel;
            _listingChannelId = NormalizeChannelId(listing);
            _delistingChannelId = NormalizeChannelId(_cfg.delisting_channel);

            if (_listingChannelId == null && _delistingChannelId == null)
            {
                _log("⚠ Обидва Channel ID порожні або некоректні у config.json.");
            }
            else
            {
                if (_listingChannelId != null) _log($"Listening LISTING channel ID: {_listingChannelId}");
                if (_delistingChannelId != null) _log($"Listening DELISTING channel ID: {_delistingChannelId}");
            }

            _client = new Client(What => What switch
            {
                "api_id" => _cfg.api_id?.ToString(),
                "api_hash" => _cfg.api_hash,
                "phone_number" => _cfg.phone_number,
                "session_pathname" => _cfg.session_pathname,
                _ => null
            });

            await _client.LoginUserIfNeeded();
            _log("Parser connected to Telegram");

            _client.OnUpdates += OnUpdates;
            _log("Parser listening...");

            _ = KeepAlive(_cts.Token);
        }

        private static long? NormalizeChannelId(string channel)
        {
            if (string.IsNullOrWhiteSpace(channel)) return null;
            channel = channel.Trim();
            if (channel.StartsWith("-100"))
            {
                var rest = channel.Substring(4);
                if (long.TryParse(rest, out var id)) return id;
                return null;
            }
            if (long.TryParse(channel, out var plain))
                return plain;
            return null;
        }

        private async Task KeepAlive(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                    await Task.Delay(1000, token);
            }
            catch { }
        }

        private static string PreviewText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            text = text.Replace("\r", " ").Replace("\n", " ");
            return text.Length > 120 ? text.Substring(0, 120) + "..." : text;
        }

        private bool AnyPatternMatchesImpl(string[] patternLines, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            
            var textNorm = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
            _log($"[Filter] Normalized text: '{textNorm}'");

            foreach (var raw in patternLines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var pattern = raw.Trim();
                _log($"[Filter] Testing pattern: '{pattern}'");

                const string tokenMarker = "___TOKEN_PLACEHOLDER___";
                var withMarker = pattern.Replace("{TOKEN}", tokenMarker);
                var escaped = System.Text.RegularExpressions.Regex.Escape(withMarker);
                escaped = escaped.Replace("\\ ", @"\s+");
                var tokenRegex = @"(?:\$)?[A-Za-z0-9_]{2,20}";
                escaped = escaped.Replace(tokenMarker, tokenRegex);

                _log($"[Filter] Built regex: '{escaped}'");

                try
                {
                    var rx = new System.Text.RegularExpressions.Regex(escaped, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    if (rx.IsMatch(textNorm))
                    {
                        _log($"[Filter] ✔ MATCH with pattern: '{pattern}'");
                        return true;
                    }
                    else
                    {
                        _log($"[Filter] ✘ no match");
                    }
                }
                catch (Exception ex)
                {
                    _log($"[Filter] ⚠ Regex error: {ex.Message}");
                }
            }

            return false;
        }
        
        private static bool AnyPatternMatches(string[] patternLines, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            
            var textNorm = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

            foreach (var raw in patternLines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var pattern = raw.Trim();

                const string tokenMarker = "___TOKEN_PLACEHOLDER___";
                var withMarker = pattern.Replace("{TOKEN}", tokenMarker);
                var escaped = System.Text.RegularExpressions.Regex.Escape(withMarker);
                escaped = escaped.Replace("\\ ", @"\s+");
                var tokenRegex = @"(?:\$)?[A-Za-z0-9_]{2,20}";
                escaped = escaped.Replace(tokenMarker, tokenRegex);

                try
                {
                    var rx = new System.Text.RegularExpressions.Regex(escaped, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    if (rx.IsMatch(textNorm)) return true;
                }
                catch
                {
                }
            }

            return false;
        }


        private bool MatchesConfiguredPatterns(string text, bool isListing)
        {
            var f = _cfg.filters;
            if (f == null)
            {
                _log("[Filter] filters == null, blocked");
                return false;
            }

            var srcPatterns = isListing ? f.listing_patterns : f.delisting_patterns;
            var srcName = isListing ? "listing_patterns" : "delisting_patterns";
            
            if (srcPatterns == null || srcPatterns.Length == 0)
            {
                _log($"[Filter] {srcName} empty, blocked");
                return false;
            }

            _log($"[Filter] Checking against {srcPatterns.Length} {srcName} patterns...");
            var matched = AnyPatternMatchesImpl(srcPatterns, text);
            _log($"[Filter] Result: {(matched ? "MATCHED ✔" : "NO MATCH ✘")}");
            
            return matched;
        }

        private static bool PassesFilter(FilterSettings filters, string action, string exchange, string market)
        {
            if (filters.rules != null && filters.rules.Count > 0)
            {
                foreach (var r in filters.rules)
                {
                    if (RulePasses(r, action, exchange, market)) return true;
                }
                return false;
            }
            else
            {
                return RulePasses(new FilterRule
                {
                    action = filters.action,
                    exchanges = filters.exchanges,
                    market = filters.market
                }, action, exchange, market);
            }
        }

        private static bool RulePasses(FilterRule rule, string action, string exchange, string market)
        {
            if (!string.IsNullOrWhiteSpace(rule.action) && !string.Equals(rule.action, action, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.IsNullOrWhiteSpace(rule.market) && !string.Equals(rule.market, market, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.IsNullOrWhiteSpace(rule.exchanges))
            {
                var parts = rule.exchanges.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                bool any = false;
                foreach (var p in parts)
                {
                    if (string.Equals(p.Trim(), exchange, StringComparison.OrdinalIgnoreCase))
                    {
                        any = true;
                        break;
                    }
                }
                if (!any) return false;
            }
            return true;
        }

        private async Task OnUpdates(UpdatesBase updates)
        {
            if (!_isRunning) return;

            switch (updates)
            {
                case Updates upd when upd.UpdateList != null:
                    foreach (var u in upd.UpdateList)
                        await HandleUpdate(u);
                    break;

                case UpdateShortMessage usm:
                    {
                        var msg = new Message
                        {
                            peer_id = new PeerUser { user_id = usm.user_id },
                            message = usm.message
                        };
                        await HandleUpdate(new UpdateNewMessage { message = msg });
                        break;
                    }

                case UpdateShortChatMessage uscm:
                    {
                        var msg = new Message
                        {
                            peer_id = new PeerChat { chat_id = uscm.chat_id },
                            message = uscm.message
                        };
                        await HandleUpdate(new UpdateNewMessage { message = msg });
                        break;
                    }

                default:
                    break;
            }

            await Task.CompletedTask;
        }

        private Task HandleUpdate(object u)
        {
            try
            {
                if (u is UpdateNewMessage unm && unm.message is Message msg)
                {
                    long cid;
                    if (msg.peer_id is PeerChannel pc)
                    {
                        cid = pc.channel_id;
                    }
                    else if (msg.peer_id is PeerChat pchat)
                    {
                        cid = pchat.chat_id;
                    }
                    else
                    {
                        return Task.CompletedTask;
                    }
                    bool isListing = _listingChannelId != null && cid == _listingChannelId.Value;
                    bool isDelisting = _delistingChannelId != null && cid == _delistingChannelId.Value;
                    var matchSrc = isListing ? "LISTING" : (isDelisting ? "DELISTING" : "NONE");
                    _log($"[Debug] UpdateNewMessage: peer={msg.peer_id.GetType().Name}, cid={cid}, match={matchSrc}");
                    if (!(isListing || isDelisting))
                        return Task.CompletedTask; 

                    var text = msg.message ?? "";
                    if (string.IsNullOrWhiteSpace(text)) { _log("[Debug] Empty text"); return Task.CompletedTask; }

                if (!MatchesConfiguredPatterns(text, isListing)) { _log("[Filter] Skipped by patterns"); return Task.CompletedTask; }

                var md = _rxDetailed.Match(text);
                    if (md.Success)
                    {
                        var token = md.Groups["t"].Value.ToUpperInvariant();
                        var token2 = md.Groups["t2"].Success ? md.Groups["t2"].Value.ToUpperInvariant() : null;
                        var action = md.Groups["action"].Value.ToLowerInvariant();
                        var exchange = md.Groups["exchange"].Value;
                        var marketRaw = md.Groups["market"].Value.ToLowerInvariant();
                        var market = (marketRaw.StartsWith("spot")) ? "spot" : "futures";

                        string src = "";
                        {
                            long cid2;
                            if (msg.peer_id is PeerChannel pc2) cid2 = pc2.channel_id; else if (msg.peer_id is PeerChat pchat2) cid2 = pchat2.chat_id; else cid2 = 0;
                            bool isL2 = _listingChannelId != null && cid2 == _listingChannelId.Value;
                            bool isD2 = _delistingChannelId != null && cid2 == _delistingChannelId.Value;
                            src = isL2 ? "LISTING" : (isD2 ? "DELISTING" : "");
                        }

                        _log($"$TOKEN [{src}] {action.ToUpperInvariant()} — {token} on {exchange} {market}");
                        _openToken(token, src);
                        if (!string.IsNullOrEmpty(token2))
                        {
                            _log($"$TOKEN [{src}] {action.ToUpperInvariant()} — {token2} on {exchange} {market}");
                            _openToken(token2, src);
                        }
                        return Task.CompletedTask;
                    }

                    if (!MatchesConfiguredPatterns(text, isListing)) { _log("[Filter] Skipped by patterns"); return Task.CompletedTask; }
                    var m = _rx.Match(text);
                    if (!m.Success) { _log($"[Debug] No $TOKEN in: '{PreviewText(text)}'"); return Task.CompletedTask; }

                    {
                        var token = m.Groups["t"].Value.ToUpperInvariant();
                        string src = "";
                        {
                            long cid2;
                            if (msg.peer_id is PeerChannel pc2) cid2 = pc2.channel_id; else if (msg.peer_id is PeerChat pchat2) cid2 = pchat2.chat_id; else cid2 = 0;
                            bool isL2 = _listingChannelId != null && cid2 == _listingChannelId.Value;
                            bool isD2 = _delistingChannelId != null && cid2 == _delistingChannelId.Value;
                            src = isL2 ? "LISTING" : (isD2 ? "DELISTING" : "");
                        }
                        _log($"$TOKEN detected [{src}]: {token}");
                        _openToken(token, src);
                        return Task.CompletedTask;
                    }

                    return Task.CompletedTask;
                }
                else if (u is UpdateNewChannelMessage uncm && uncm.message is Message msg2)
                {
                    long cid;
                    if (msg2.peer_id is PeerChannel pc)
                    {
                        cid = pc.channel_id;
                    }
                    else if (msg2.peer_id is PeerChat pchat)
                    {
                        cid = pchat.chat_id;
                    }
                    else
                    {
                        return Task.CompletedTask;
                    }
                    bool isListing = _listingChannelId != null && cid == _listingChannelId.Value;
                    bool isDelisting = _delistingChannelId != null && cid == _delistingChannelId.Value;
                    var matchSrc = isListing ? "LISTING" : (isDelisting ? "DELISTING" : "NONE");
                    _log($"[Debug] UpdateNewChannelMessage: peer={msg2.peer_id.GetType().Name}, cid={cid}, match={matchSrc}");
                    if (!(isListing || isDelisting))
                        return Task.CompletedTask; 

                    var text = msg2.message ?? "";
                    if (string.IsNullOrWhiteSpace(text)) { _log("[Debug] Empty text"); return Task.CompletedTask; }

                    if (!MatchesConfiguredPatterns(text, isListing)) { _log("[Filter] Skipped by patterns"); return Task.CompletedTask; }

                    var md = _rxDetailed.Match(text);
                    if (md.Success)
                    {
                        var token = md.Groups["t"].Value.ToUpperInvariant();
                        var token2 = md.Groups["t2"].Success ? md.Groups["t2"].Value.ToUpperInvariant() : null;
                        var action = md.Groups["action"].Value.ToLowerInvariant();
                        var exchange = md.Groups["exchange"].Value;
                        var marketRaw = md.Groups["market"].Value.ToLowerInvariant();
                        var market = (marketRaw.StartsWith("spot")) ? "spot" : "futures";

                        if (_cfg.filters != null && _cfg.filters.enabled)
                        {
                            if (!((_cfg.filters.listing_patterns?.Length > 0) || (_cfg.filters.delisting_patterns?.Length > 0)))
                            {
                                if (!PassesFilter(_cfg.filters, action, exchange, market))
                                {
                                    _log($"[Filter] Skipped {token} ({action} on {exchange} {market})");
                                    return Task.CompletedTask;
                                }
                            }
                        }

                        string src = "";
                        {
                            long cid2;
                            if (msg2.peer_id is PeerChannel pc2) cid2 = pc2.channel_id; else if (msg2.peer_id is PeerChat pchat2) cid2 = pchat2.chat_id; else cid2 = 0;
                            bool isL2 = _listingChannelId != null && cid2 == _listingChannelId.Value;
                            bool isD2 = _delistingChannelId != null && cid2 == _delistingChannelId.Value;
                            src = isL2 ? "LISTING" : (isD2 ? "DELISTING" : "");
                        }

                        _log($"$TOKEN [{src}] {action.ToUpperInvariant()} — {token} on {exchange} {market}");
                        _openToken(token, src);
                        if (!string.IsNullOrEmpty(token2))
                        {
                            _log($"$TOKEN [{src}] {action.ToUpperInvariant()} — {token2} on {exchange} {market}");
                            _openToken(token2, src);
                        }
                        return Task.CompletedTask;
                    }

                    if (!MatchesConfiguredPatterns(text, isListing)) { _log("[Filter] Skipped by patterns"); return Task.CompletedTask; }
                    var m = _rx.Match(text);
                    if (!m.Success) { _log($"[Debug] No $TOKEN in: '{PreviewText(text)}'"); return Task.CompletedTask; }
                    {
                        var token = m.Groups["t"].Value.ToUpperInvariant();
                        string src = "";
                        {
                            long cid2;
                            if (msg2.peer_id is PeerChannel pc2) cid2 = pc2.channel_id; else if (msg2.peer_id is PeerChat pchat2) cid2 = pchat2.chat_id; else cid2 = 0;
                            bool isL2 = _listingChannelId != null && cid2 == _listingChannelId.Value;
                            bool isD2 = _delistingChannelId != null && cid2 == _delistingChannelId.Value;
                            src = isL2 ? "LISTING" : (isD2 ? "DELISTING" : "");
                        }
                        _log($"$TOKEN detected [{src}]: {token}");
                        _openToken(token, src);
                        return Task.CompletedTask;
                    }
                }
            }
            catch (Exception ex)
            {
                _log("Update error: " + ex.Message);
            }

            return Task.CompletedTask;
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _log("Stopping parser...");
            _isRunning = false;
            _cts?.Cancel();

            if (_client != null)
            {
                _client.OnUpdates -= OnUpdates;
                _client.Dispose();
                _client = null;
            }

            _log("Parser stopped.");
        }
    }
}

