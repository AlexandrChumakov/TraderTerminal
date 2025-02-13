namespace Trader.Services;

public static class TimeConverter
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

    public static string ConvertSecondsToIntervalStrict(this int seconds) =>
        SecondsToInterval.GetValueOrDefault(seconds, "1m");
}