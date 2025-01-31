using Trader.Interfaces;
using Trader.Models;

namespace Trader;

public class BitfinexConnector(IRestClient restClient) : IBitfinexConnector
{
    public Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount) =>
        restClient.GetNewTradesAsync(pair, maxCount);

    public Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from,
        long? count = 0, DateTimeOffset? to = null) =>
        restClient.GetCandleSeriesAsync(pair, periodInSec, from, count, to);


    public event Action<Trade>? NewBuyTrade;
    public event Action<Trade>? NewSellTrade;

    public void SubscribeTrades(string pair, int maxCount = 100)
    {
    }

    public void UnsubscribeTrades(string pair)
    {
    }

    public event Action<Candle>? CandleSeriesProcessing;

    public void SubscribeCandles(string pair, int periodInSec,
        long? count = 0, DateTimeOffset? from = null, DateTimeOffset? to = null)
    {
    }

    public void UnsubscribeCandles(string pair)
    {
    }
}