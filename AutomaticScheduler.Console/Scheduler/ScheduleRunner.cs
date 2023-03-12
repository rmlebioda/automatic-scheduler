using AutomaticScheduler.Console.Scheduler.States;
using Microsoft.Extensions.Logging;
using PriceCheckWebScrapper.Exceptions;

namespace AutomaticScheduler.Console.Scheduler;

public class ScheduleRunner<Service, TimerState> where TimerState : BaseTimerState
{
    private readonly ILogger _logger;
    private readonly MailManager.MailManager _mailManager;
    private readonly Options _options;
    private readonly Service _service;
    private readonly Func<TimerState, Task> _taskToExecute;
    private readonly Func<Options, Service, TimerState> _timerStateCreator;

    public ScheduleRunner(Service service, Options options, ILogger logger, MailManager.MailManager mailManager,
        Func<Options, Service, TimerState> timerStateCreator, Func<TimerState, Task> taskToExecute)
    {
        _service = service;
        _options = options;
        _logger = logger;
        _mailManager = mailManager;
        _timerStateCreator = timerStateCreator;
        _taskToExecute = taskToExecute;
    }

    public Timer RunScheduler()
    {
        var dueTime = _options.StartDateTime is null
            ? TimeSpan.Zero
            : DateTime.Now - DateTime.Parse(_options.StartDateTime);
        _logger.LogInformation("Task is scheduled in {DueTime} minutes", dueTime.TotalMinutes.ToString());

        // ReSharper disable once AsyncVoidLambda
        return new Timer(async state =>
            {
                var stateAsTimer = (TimerState) state!;
                TryToSendOverdueErrors(stateAsTimer);
                try
                {
                    _logger.LogInformation(
                        "Starting executing scheduled task with repeating period of {Period} minutes",
                        _options.Interval.ToString());
                    await _taskToExecute(stateAsTimer);
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        "Unhandled error occurred during executing timer: {Exception}",
                        e.ToString());
                    stateAsTimer.OverdueNotSentErrors.Add(e.ToString());

                    if (e is not EmailSendReportException)
                        TryToSendException(stateAsTimer, e);
                }
                finally
                {
                    _logger.LogInformation("Finished executing scheduled task");
                }
            },
            _timerStateCreator(_options, _service),
            dueTime,
            TimeSpan.FromMinutes(_options.Interval));
    }

    private void TryToSendException(TimerState state, Exception e)
    {
        _logger.LogInformation("Trying to send exception with email");
        try
        {
            _mailManager.SendEmail(
                _options.EmailManagerTargetEmail,
                "Unhandled exception in executing timer",
                e.ToString());
        }
        catch (Exception exception)
        {
            _logger.LogInformation("Sending email failed with exception: {Exception}",
                exception.ToString());
            state.OverdueNotSentErrors.Add(exception.ToString());
        }
    }

    private void TryToSendOverdueErrors(TimerState state)
    {
        _logger.LogInformation("Overdue errors count: {ErrorCount}",
            state.OverdueNotSentErrors.Count);

        if (!state.OverdueNotSentErrors.Any())
            return;

        try
        {
            _logger.LogInformation("Sending overdue errors");
            _mailManager.SendEmail(
                _options.EmailManagerTargetEmail,
                "Overdue errors",
                $"There are {state.OverdueNotSentErrors.Count} overdue errors:{Environment.NewLine}"
                + string.Join(Environment.NewLine,
                    state.OverdueNotSentErrors.Select((error, index) => $"===> Error {index}: {error}")));
            _logger.LogInformation("Overdue errors sent, clearing...");
            state.OverdueNotSentErrors.Clear();
        }
        catch (Exception e)
        {
            _logger.LogError(
                "Failed to send overdue errors: {Exception}",
                e.ToString());
            state.OverdueNotSentErrors.Add(e.ToString());
        }
    }
}