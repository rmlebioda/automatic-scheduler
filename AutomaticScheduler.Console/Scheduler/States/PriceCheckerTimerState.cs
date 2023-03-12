using PriceCheckWebScrapper;

namespace AutomaticScheduler.Console.Scheduler.States;

internal record PriceCheckerTimerState : BaseTimerState
{
    public PriceChecker PriceChecker { get; init; }
    public Options Options { get; init; }
}