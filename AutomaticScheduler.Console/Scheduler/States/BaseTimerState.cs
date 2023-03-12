namespace AutomaticScheduler.Console.Scheduler.States;

public record BaseTimerState()
{
    public List<string> OverdueNotSentErrors { get; init; } = new List<string>();
}