using CinemaCity;

namespace AutomaticScheduler.Console.Scheduler.States;

public record CinemaCityTimerState() : BaseTimerState
{
    public CinemaCityService CinemaCityService { get; init; }
    public Options Options { get; init; }
}