using AutomaticScheduler.Console.Scheduler.States;
using CinemaCity;
using Microsoft.Extensions.Logging;

namespace AutomaticScheduler.Console.Scheduler;

public class CinemaCityScheduler : IScheduler
{
    private readonly CinemaCityOptions _cinemaCityOptions;
    private readonly ILogger _logger;
    private readonly CinemaCityService _service;
    private readonly List<Uri> _supportedUris;
    private Timer? _scheduler;

    public CinemaCityScheduler(CinemaCityOptions cinemaCityOptions, ILogger logger, List<Uri> supportedUris)
    {
        _cinemaCityOptions = cinemaCityOptions;
        _logger = logger;
        _supportedUris = supportedUris;

        _service = new CinemaCityService(_cinemaCityOptions, _logger, _supportedUris);
    }

    public Timer RunScheduler(Options options)
    {
        if (_scheduler is not null)
            return _scheduler;

        var runner = new ScheduleRunner<CinemaCityService, CinemaCityTimerState>(
            _service,
            options,
            _logger,
            _cinemaCityOptions.MailManager,
            CreateTimerState,
            TaskToExecute);

        _scheduler = runner.RunScheduler();

        return _scheduler;
    }

    private async Task TaskToExecute(CinemaCityTimerState state)
    {
        await state.CinemaCityService.CheckAsync();
    }

    private CinemaCityTimerState CreateTimerState(Options options, CinemaCityService service)
    {
        return new CinemaCityTimerState
        {
            Options = options,
            CinemaCityService = service
        };
    }
}