using PriceCheckWebScrapper;

namespace AutomaticScheduler.Console;

internal record TimerState
{
    public PriceChecker PriceChecker { get; init; }
    public Options Options { get; init; }
}