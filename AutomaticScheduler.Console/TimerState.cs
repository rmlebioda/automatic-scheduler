using PriceCheckWebScrapper;

namespace AutomaticScheduler.Console;

internal record TimerState
{
    public PriceChecker PriceChecker { get; init; }
    public Options Options { get; init; }
    public List<string> OverdueNotSentErrors { get; init; } = new List<string>();
}