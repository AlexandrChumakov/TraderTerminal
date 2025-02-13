namespace Trader.Interfaces;

public interface ICurrencyConverter
{
    Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency);
}