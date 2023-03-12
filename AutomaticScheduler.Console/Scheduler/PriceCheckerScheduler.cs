using AutomaticScheduler.Console.Scheduler.States;
using Microsoft.Extensions.Logging;
using PriceCheckWebScrapper;

namespace AutomaticScheduler.Console.Scheduler;

public class PriceCheckerScheduler : IScheduler
{
    private readonly PriceChecker _checker;
    private readonly PriceCheckerOptions _checkerOptions;
    private readonly ILogger _logger;
    private readonly List<Uri> _supportedUris;
    private Timer? _scheduler;

    public PriceCheckerScheduler(PriceCheckerOptions checkerOptions, ILogger logger, List<Uri> supportedUris)
    {
        _checkerOptions = checkerOptions;
        _logger = logger;
        _supportedUris = supportedUris;
        _checker = new PriceChecker(_checkerOptions, _logger);
    }

    public Timer RunScheduler(Options options)
    {
        if (_scheduler is not null)
            return _scheduler;

        var runner = new ScheduleRunner<PriceChecker, PriceCheckerTimerState>(
            _checker,
            options,
            _logger,
            _checkerOptions.MailManager,
            CreateTimerState,
            TaskToExecute);

        _scheduler = runner.RunScheduler();

        return _scheduler;
    }

    private async Task TaskToExecute(PriceCheckerTimerState state)
    {
        await state.PriceChecker.CheckAsync(_supportedUris);
    }

    private PriceCheckerTimerState CreateTimerState(Options options, PriceChecker checker)
    {
        return new PriceCheckerTimerState
        {
            Options = options,
            PriceChecker = checker
        };
    }
}