namespace Trader.Interfaces;

public interface IPortfolioCalculator
{
    Task<Dictionary<string, decimal>> CalculatePortfolioValuesAsync(
        Dictionary<string, decimal> portfolio, string[] targetCurrencies);
}