using Trader.Interfaces;
using Trader.Models;

namespace Trader;

public class Connector : IConnector
{
    private readonly IWsClient _wsClient;
    private readonly IRestClient _restClient;
    private readonly Dictionary<string, int> _tradesCounter = new();
    private readonly Dictionary<string, int> _tradesMaxCounter = new();

    private readonly Dictionary<string, int> _candelesCounter = new();
    private readonly Dictionary<string, long?> _candelesMaxCounter = new();

    public Connector(IWsClient wsClient, IRestClient restClient)
    {
        _restClient = restClient;
        _wsClient = wsClient;

        _wsClient.TradeReceived += OnTradeReceived;
        _wsClient.CandleReceived += OnCandleReceived;
    }


    public Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount) =>
        _restClient.GetNewTradesAsync(pair, maxCount);

    public Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from,
        long? count = 0, DateTimeOffset? to = null) =>
        _restClient.GetCandleSeriesAsync(pair, periodInSec, from, count, to);


    public event Action<Trade>? NewBuyTrade;
    public event Action<Trade>? NewSellTrade;

    public void SubscribeTrades(string pair, int maxCount = 100)
    {
        _tradesCounter[pair] = 0;
        _tradesMaxCounter[pair] = maxCount;

        _wsClient.SubscribeTrades(pair);
    }

    public void UnsubscribeTrades(string pair)
    {
        _tradesCounter.Remove(pair);
        _tradesMaxCounter.Remove(pair);

        _wsClient.UnsubscribeTrades(pair);
    }

    public event Action<Candle>? CandleSeriesProcessing;

    public void SubscribeCandles(string pair, int periodInSec,
        long? count = 0, DateTimeOffset? from = null, DateTimeOffset? to = null)
    {
        _candelesCounter[pair] = 0;
        _candelesMaxCounter[pair] = count;
    }

    public void UnsubscribeCandles(string pair)
    {
        _candelesCounter.Remove(pair);
        _candelesMaxCounter.Remove(pair);

        _wsClient.UnsubscribeCandles(pair);
    }

    private void OnCandleReceived(Candle candle)
    {
        if (!_candelesCounter.ContainsKey(candle.Pair)) return;

        _candelesCounter[candle.Pair]++;

        if (_candelesCounter[candle.Pair] <= _candelesMaxCounter[candle.Pair])
            CandleSeriesProcessing?.Invoke(candle);
        else
            UnsubscribeCandles(candle.Pair);
    }

    private void OnTradeReceived(Trade trade)
    {
        if (!_tradesCounter.ContainsKey(trade.Pair)) return;

        _tradesCounter[trade.Pair]++;

        if (_tradesCounter[trade.Pair] <= _tradesMaxCounter[trade.Pair])
        {
            if (trade.Side == "buy")
                NewBuyTrade?.Invoke(trade);
            else
                NewSellTrade?.Invoke(trade);
        }
        else
            UnsubscribeTrades(trade.Pair);
    }
}