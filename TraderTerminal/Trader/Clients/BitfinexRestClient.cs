using Polly.Retry;
using RestSharp;
using Trader.Mapping;
using Trader.Models;
using IRestClient = Trader.Interfaces.IRestClient;

namespace Trader.Clients;

public class BitfinexRestClient : IRestClient
{
    private const string Api = "https://api-pub.bitfinex.com/v2/";
    private static readonly AsyncRetryPolicy<RestResponse> Policy = BitfinexRestMapping.GetPolicy();

    public async Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, long maxCount)
    {
        var client = new RestClient(new RestClientOptions(Api));
        var request = new RestRequest($"trades/t{pair}/hist?limit={maxCount}");
        request.AddHeader("accept", "application/json");

        var response = await ExecuteWithRetryAsync(client, request);

        return response.Content!.ToTrades(pair);
    }

    public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from,
        long? count, DateTimeOffset? to = null)
    {
        var client = new RestClient(new RestClientOptions(Api));
        var request = new RestRequest($"candles/trade%3A{periodInSec.ConvertSecondsToIntervalStrict()}%3At{pair}");


        if (from.HasValue)
            request.AddQueryParameter("start", from.Value.ToUnixTimeMilliseconds());
        if (to.HasValue)
            request.AddQueryParameter("end", to.Value.ToUnixTimeMilliseconds());
        if (count.HasValue)
            request.AddQueryParameter("limit", count.Value);

        request.AddHeader("accept", "application/json");

        var response = await ExecuteWithRetryAsync(client, request);

        return response.Content!.ToCandels(pair);
    }

    public async Task<Ticker> GetTickerInfo(string pair)
    {
        var client = new RestClient(new RestClientOptions(Api));
        var request = new RestRequest($"ticker/{pair}");
        var response = await ExecuteWithRetryAsync(client, request);
        return response.Content!.ToTicker(pair);
    }

    private static async Task<RestResponse> ExecuteWithRetryAsync(
        RestClient client, RestRequest request)
    {
        var response = await Policy.ExecuteAsync(async () => await client.ExecuteAsync(request));

        if (response is null)
            throw new HttpRequestException("No response received from Bitfinex API.");

        if (response.ErrorException is not null)
            throw new HttpRequestException("Error occurred while calling Bitfinex API.", response.ErrorException);

        if (!response.IsSuccessful)
            throw new HttpRequestException(
                $"Bitfinex API returned status {response.StatusCode}: {response.StatusDescription}");

        return response;
    }
}