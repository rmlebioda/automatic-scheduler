namespace AutomaticScheduler.Console.Scheduler;

public interface IScheduler
{
    Timer RunScheduler(Options options);
}
