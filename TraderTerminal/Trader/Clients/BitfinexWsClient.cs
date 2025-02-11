using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Trader.Interfaces;
using Trader.Mapping;
using Trader.Models;
using Trader.Services;

namespace Trader.Clients;

public class BitfinexWsClient : IWsClient
{
    private ClientWebSocket? _ws;
    private CancellationTokenSource _cts;
    private const string Uri = "wss://api-pub.bitfinex.com/ws/2";

    private readonly HashSet<string> _tradePairs = [];

    private readonly HashSet<(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to
            )>
        _candlePairs = [];

    private readonly Dictionary<int, SubscriptionInfo> _subscriptions = new();

    public event Action<Trade>? TradeReceived;
    public event Action<Candle>? CandleReceived;

    #region Connection

    private async Task ConnectAsync()
    {
        await Reconnect.GetWcPolicy().ExecuteAsync(async () =>
        {
            _ws = new ClientWebSocket();
            _cts = new CancellationTokenSource();
            await _ws.ConnectAsync(new Uri(Uri), _cts.Token);

            _ = Task.Run(ListenLoop, _cts.Token);
        });
        await RestoreSubscriptionsAsync();
    }

    private async Task EnsureConnectedAndSendAsync(Action sendAction)
    {
        if (!IsWebSocketOpen())
            await ConnectAsync();

        sendAction();
    }

    private async Task ReconnectAsync()
    {
        if (_ws != null && _ws.State != WebSocketState.Closed)
        {
            try
            {
                _ws.Abort();
            }
            catch
            {
                // ignored
            }

            _ws.Dispose();
            await ConnectAsync();
        }
    }

    private async Task RestoreSubscriptionsAsync()
    {
        foreach (var traidePair in _tradePairs)
        {
            await Task.Delay(100, _cts.Token);
            SendTradesRequest(traidePair, "subscribe");
        }

        foreach (var candlePair in _candlePairs)
        {
            await Task.Delay(100, _cts.Token);
            SendCandlesRequest(candlePair.pair, candlePair.periodInSec, "subscribe", candlePair.from,
                candlePair.to);
        }
    }

    private void Send(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        _ = _ws!.SendAsync(bytes, WebSocketMessageType.Text, true, _cts.Token);
    }

    private bool IsWebSocketOpen()
    {
        return _ws is { State: WebSocketState.Open };
    }

    #endregion

    #region Trades

    public void SubscribeTrades(string pair)
    {
        _tradePairs.Add(pair);

        _ = EnsureConnectedAndSendAsync(() => SendTradesRequest(pair, "subscribe"));
    }

    public void UnsubscribeTrades(string pair)
    {
    }

    private void SendTradesRequest(string pair, string type)
    {
        if (!IsWebSocketOpen()) return;

        var symbolPair = 't' + pair.ToUpper();

        var request = new
        {
            @event = type,
            channel = "trades",
            pair = symbolPair
        };

        var json = JsonSerializer.Serialize(request);
        Send(json);
    }

    #endregion

    #region Candeles

    public void SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to)
    {
        _candlePairs.Add((pair, periodInSec, from, to));

        _ = EnsureConnectedAndSendAsync(() => SendCandlesRequest(pair, periodInSec, "subscribe", from, to));
    }

    public void UnsubscribeCandles(string pair)
    {
        throw new NotImplementedException();
    }

    private void SendCandlesRequest(string pair, int periodInSec, string type, DateTimeOffset? from,
        DateTimeOffset? to)
    {
        if (!IsWebSocketOpen()) return;

        var symbolPair = 't' + pair.ToUpper();
        var requestStr = $"trade:{periodInSec.ConvertSecondsToIntervalStrict()}:{symbolPair}";

        if (from.HasValue)
            requestStr = $"{requestStr}:p{from.Value.ToUnixTimeMilliseconds()}";
        if (to.HasValue)
            requestStr = $"{requestStr}:p{to.Value.ToUnixTimeMilliseconds()}";


        var request = new
        {
            @event = type,
            chenel = "candles",
            key = requestStr
        };
        var json = JsonSerializer.Serialize(request);

        Send(json);
    }

    #endregion

    #region Respones

    private async Task ListenLoop()
    {
        var buffer = new byte[8192];

        try
        {
            while (_ws!.State == WebSocketState.Open)
            {
                var result = await _ws.ReceiveAsync(buffer, _cts.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                HandleIncomingMessage(json);
            }
        }
        finally
        {
            await Task.Delay(1500, _cts.Token);
            await ReconnectAsync();
        }
    }


    private void HandleIncomingMessage(string json)
    {
        if (json.StartsWith('{'))
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("event", out var eventProp)) return;
            var eventType = eventProp.GetString();
            if (eventType != "subscribed") return;
            var channel = root.GetProperty("channel").GetString();
            var chanId = root.GetProperty("chanId").GetInt32();
            var keyOrPair = channel == "candles"
                ? root.GetProperty("key").GetString()
                : root.GetProperty("pair").GetString();

            _subscriptions[chanId] = new SubscriptionInfo
            {
                Channel = channel,
                KeyOrPair = keyOrPair
            };
        }
        else if (json.StartsWith('['))
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                var arr = root.EnumerateArray().ToArray();
                if (arr.Length < 2)
                    return;

                var chanId = arr[0].GetInt32();
                if (arr[1].ValueKind == JsonValueKind.String && arr[1].GetString() == "hb")
                    return;

                if (!_subscriptions.TryGetValue(chanId, out var subInfo)) return;
                switch (subInfo.Channel)
                {
                    case "candles" when arr[1].ValueKind != JsonValueKind.Array:
                        return;
                    case "candles":
                    {
                        using var enumerator = arr[1].EnumerateArray();
                        if (enumerator.Any() && enumerator.First().ValueKind == JsonValueKind.Array)
                        {
                            foreach (var candleElement in arr[1].EnumerateArray())
                                CandleReceived?.Invoke(candleElement.ToCandle(subInfo));
                        }
                        else
                        {
                            CandleReceived?.Invoke(arr[1].ToCandle(subInfo));
                        }

                        break;
                    }
                    case "trades" when arr[1].ValueKind == JsonValueKind.String:
                        TradeReceived?.Invoke(arr[2].ToTrade(subInfo));
                        break;
                    case "trades":
                    {
                        if (arr[1].ValueKind == JsonValueKind.Array)
                        {
                            foreach (var tradeElement in arr[1].EnumerateArray())
                            {
                                TradeReceived?.Invoke(tradeElement.ToTrade(subInfo));
                            }
                        }

                        break;
                    }
                }
            }
        }
    }

    #endregion
}