namespace AutomaticScheduler.Console;

public class TimedTaskScheduler
{
    public TimedTaskScheduler()
    {
    }

    /// <summary>
    /// Runs given action and repeats it after some period of time
    /// </summary>
    /// <param name="actionToExecute">Action to execute</param>
    /// <param name="repeatExecutionAfterCompletion">Period of time after which task will be executed again</param>
    /// <param name="firstExecution">Period of time for first execution (null starts execution immediately)</param>
    public void PeriodicallyRunTask(Action actionToExecute, TimeSpan repeatExecutionAfterCompletion,
        TimeSpan? firstExecution = null, CancellationToken? cancellationToken = null)
    {
        RegisterTask(actionToExecute, repeatExecutionAfterCompletion, firstExecution ?? TimeSpan.Zero, cancellationToken);
    }


    private void RegisterTask(Action action, TimeSpan repeatExecutionAfterCompletion,
        TimeSpan firstExecution, CancellationToken? cancellationToken)
    {
    }
}