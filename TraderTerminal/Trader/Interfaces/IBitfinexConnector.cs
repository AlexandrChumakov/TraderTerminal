using Trader.Models;

namespace Trader.Interfaces;

public interface IBitfinexConnector
{
    Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount);

    Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from,
        long? count = 0, DateTimeOffset? to = null);


    event Action<Trade> NewBuyTrade;
    event Action<Trade> NewSellTrade;

    void SubscribeTrades(string pair, int maxCount = 100);
    void UnsubscribeTrades(string pair);

    event Action<Candle> CandleSeriesProcessing;

    void SubscribeCandles(string pair, int periodInSec,
        long? count = 0, DateTimeOffset? from = null, DateTimeOffset? to = null);

    void UnsubscribeCandles(string pair);
}