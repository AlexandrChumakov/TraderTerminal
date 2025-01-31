namespace Trader.Models;

public class Trade
{
    public string Id { get; set; }

    public string Pair { get; set; }

    public decimal Price { get; set; }

    public decimal Amount { get; set; }

    public string Side { get; set; }

    public DateTimeOffset Time { get; set; }
}