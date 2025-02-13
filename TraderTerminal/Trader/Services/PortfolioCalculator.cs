using Trader.Interfaces;

namespace Trader.Services;

public class PortfolioCalculator(ICurrencyConverter currencyConverter) : IPortfolioCalculator
{
    public async Task<Dictionary<string, decimal>> CalculatePortfolioValuesAsync(Dictionary<string, decimal> portfolio,
        string[] targetCurrencies)
    {
        var results = new Dictionary<string, decimal>();

        foreach (var target in targetCurrencies)
        {
            var total = 0m;
            foreach (var asset in portfolio)
            {
                var converted = await currencyConverter.ConvertAsync(asset.Value, asset.Key, target);
                total += converted;
            }

            results[target] = total;
        }

        return results;
    }
}