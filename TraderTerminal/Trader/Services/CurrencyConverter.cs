using Trader.Interfaces;

namespace Trader.Services;

public class CurrencyConverter(IRestClient restClient) : ICurrencyConverter
{
    private string MapToBitfinexSymbol(string currency)
    {
        if (currency.Equals("DASH", StringComparison.OrdinalIgnoreCase))
            return "DSH";

        return currency.Equals("USDT", StringComparison.OrdinalIgnoreCase) ? "USD" : currency.ToUpper();
    }


    public async Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency)
    {
        if (fromCurrency.Equals(toCurrency, StringComparison.OrdinalIgnoreCase))
            return amount;
        var fromRate = 1m;
        if (!fromCurrency.Equals("USDT", StringComparison.OrdinalIgnoreCase))
        {
            var fromSymbol = MapToBitfinexSymbol(fromCurrency);
            var tickerFrom = await restClient.GetTickerInfo(fromSymbol + "USD");
            fromRate = tickerFrom.LastPrice;
        }

        var toRate = 1m;
        if (!toCurrency.Equals("USDT", StringComparison.OrdinalIgnoreCase))
        {
            var toSymbol = MapToBitfinexSymbol(toCurrency);
            var tickerTo = await restClient.GetTickerInfo(toSymbol + "USD");
            toRate = tickerTo.LastPrice;
        }

        var amountInUSD = amount * fromRate;
        var converted = amountInUSD / toRate;
        return converted;
    }
}