using System.Text.Json;
using Trader.Models;

namespace Trader.Mapping;

public static class BitfinexWcMapping
{
    public static Trade ToTrade(this JsonElement tradeData, SubscriptionInfo info)
    {
        var elements = tradeData.EnumerateArray().ToArray();
        var amount = elements[2].GetDecimal();
        var side = amount > 0 ? "buy" : "sell";
        return new Trade
        {
            Id = elements[0].GetString()!,
            Pair = info.KeyOrPair,
            Time = DateTimeOffset.FromUnixTimeMilliseconds(elements[1].GetInt64()),
            Amount = amount,
            Price = elements[3].GetDecimal(),
            Side = side
        };
    }

    public static Candle ToCandle(this JsonElement candleData, SubscriptionInfo info)
    {
        var elements = candleData.EnumerateArray().ToArray();

        return new Candle
        {
            Pair = info.KeyOrPair,
            OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(elements[0].GetInt64()),
            OpenPrice = elements[1].GetDecimal(),
            ClosePrice = elements[2].GetDecimal(),
            HighPrice = elements[3].GetDecimal(),
            LowPrice = elements[4].GetDecimal(),
            TotalVolume = elements[5].GetDecimal(),
            TotalPrice = elements[2].GetDecimal() * elements[5].GetDecimal()
        };
    }
}