using System.Text.Json;
using Polly;
using Polly.Retry;
using RestSharp;
using Trader.Models;

namespace Trader.Mapping;

public static class BitfinexRestMapping
{
    private static readonly Dictionary<int, string> SecondsToInterval = new()
    {
        [60] = "1m",
        [300] = "5m",
        [900] = "15m",
        [1_800] = "30m",
        [3_600] = "1h",
        [10_800] = "3h",
        [21_600] = "6h",
        [43_200] = "12h",
        [86_400] = "1D",
        [604_800] = "1W",
        [1_209_600] = "14D",
        [2_592_000] = "1M"
    };

    public static AsyncRetryPolicy<RestResponse> GetPolicy()
    {
        return Policy.Handle<HttpRequestException>().OrResult<RestResponse>(r =>
                !r.IsSuccessful && (int)r.StatusCode >= 500 && (int)r.StatusCode < 600)
            .WaitAndRetryAsync(retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
    }

    public static IEnumerable<Trade> ToTrades(this string json, string pair)
    {
        var rows = JsonSerializer.Deserialize<List<decimal[]>>(json);

        if (rows is null)
            yield break;

        foreach (var items in rows)
        {
            if (items.Length < 4)
                continue;
            var side = items[2] > 0 ? "buy" : "sell";

            yield return new Trade
            {
                Id = ((long)items[0]).ToString(),
                Time = DateTimeOffset.FromUnixTimeSeconds((long)(items[1] / 1000)),
                Amount = items[2],
                Price = items[3],
                Pair = pair,
                Side = side
            };
        }
    }


    public static IEnumerable<Candle> ToCandels(this string json, string pair)
    {
        var rows = JsonSerializer.Deserialize<List<decimal[]>>(json);

        if (rows is null)
            yield break;

        foreach (var items in rows)
        {
            if (items.Length < 6)
                continue;

            yield return new Candle
            {
                Pair = pair,
                OpenPrice = items[1],
                HighPrice = items[3],
                LowPrice = items[4],
                ClosePrice = items[2],
                TotalVolume = items[5],
                OpenTime = DateTimeOffset.FromUnixTimeSeconds((long)(items[1] / 1000)),
                TotalPrice = items[2] * items[5]
            };
        }
    }

    public static Ticker ToTicker(this string json, string pair)
    {
        var items = JsonSerializer.Deserialize<decimal[]>(json);

        if (items!.Length < 10)
            return new Ticker();

        return new Ticker
        {
            Pair = pair,
            Bid = items[0],
            BidSize = items[1],
            Ask = items[2],
            AskSize = items[3],
            DailyChange = items[4],
            DailyChangeRelative = items[5],
            LastPrice = items[6],
            Volume = items[7],
            High = items[8],
            Low = items[9]
        };
    }

    public static string ConvertSecondsToIntervalStrict(this int seconds) =>
        SecondsToInterval.GetValueOrDefault(seconds, "1m");
}