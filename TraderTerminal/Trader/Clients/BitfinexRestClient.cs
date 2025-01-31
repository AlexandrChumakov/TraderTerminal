using Trader.Interfaces;
using Trader.Models;

namespace Trader.Clients;

public class BitfinexRestClient : IRestClient
{
    private const string Api = "https://api-pub.bitfinex.com/v2/";

    public async Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, long maxCount)
    {
        try
        {
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from,
        long? count, DateTimeOffset? to = null)
    {
      
    }

    public async Task<Ticker> GetTickerInfo(string pair)
    {
    }
}