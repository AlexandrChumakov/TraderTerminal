using Trader.Models;

namespace Trader.Interfaces;

public interface IWsClient
{
    event Action<Trade> TradeReceived;
    event Action<Candle> CandleReceived;

    void SubscribeTrades(string pair);
    void UnsubscribeTrades(string pair);

    void SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to);
    void UnsubscribeCandles(string pair);
}