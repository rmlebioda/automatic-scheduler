using AutomaticScheduler.Console;
using AutomaticScheduler.Console.Scheduler;
using CinemaCity;
using CommandLine;
using Microsoft.Extensions.Logging;
using PriceCheckWebScrapper;
using PriceCheckWebScrapper.Exceptions;
using Serilog;

var parserResult = Parser.Default.ParseArguments<Options>(args);
parserResult
    .WithParsed(Start)
    .WithNotParsed(error => Console.Error.WriteLine(string.Join(Environment.NewLine, error.ToString())));

void Start(Options options)
{
    Console.WriteLine($"Launched program, set variables: {options}");
    var serilogLogger = options.CreateSerilogLogger();

    var mailManager = new MailManager.MailManager(options.GetEmailProvider(),
        options.EmailManagerSenderEmail,
        options.EmailManagerSenderPassword);

    var logger = LoggerFactory.Create(configuration =>
        {
            configuration.AddSerilog(serilogLogger);
            if (options.WriteToConsole)
                configuration.AddConsole();
        }
    ).CreateLogger("");
    logger.LogInformation("Started application");
    logger.LogInformation("Set options: {Options}", options);

    var schedulers = new List<IScheduler>();

    AddPriceCheckerScheduler();
    AddCinemaCityScheduler();

    var timers = schedulers.AsParallel().Select(scheduler => scheduler.RunScheduler(options)).ToList();

    while (true)
        Thread.Sleep(Timeout.Infinite);
    // ReSharper disable once FunctionNeverReturns

    void AddPriceCheckerScheduler()
    {
        var supportedUris = options.UrlsToCheck
            .Select(url => new Uri(url))
            .Where(PriceChecker.IsSupported).ToList();
        
        if (!supportedUris.Any())
            return;

        var checkerOptions = options.CreatePriceCheckerOptions(mailManager);
        schedulers.Add(new PriceCheckerScheduler(checkerOptions, logger, supportedUris));
    }

    void AddCinemaCityScheduler()
    {
        var supportedUris = options.UrlsToCheck
            .Select(url => new Uri(url))
            .Where(CinemaCityService.IsSupported).ToList();
        
        if (!supportedUris.Any())
            return;

        var cinemaCityOptions = options.CreateCinemaCityOptions(mailManager);
        schedulers.Add(new CinemaCityScheduler(cinemaCityOptions, logger, supportedUris));
    }
}