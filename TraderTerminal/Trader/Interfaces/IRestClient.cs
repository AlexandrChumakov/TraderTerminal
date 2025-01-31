using Trader.Models;

namespace Trader.Interfaces;

public interface IRestClient
{
    Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, long maxCount);

    Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from,
        long? count = 0, DateTimeOffset? to = null);

    Task<Ticker> GetTickerInfo(string pair);
}