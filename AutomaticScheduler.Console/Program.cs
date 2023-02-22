using AutomaticScheduler.Console;
using CommandLine;
using MailManager;
using Microsoft.Extensions.Logging;
using PriceCheckWebScrapper;
using Serilog;
using Serilog.Core;

PriceCheckerOptions CreatePriceCheckerOptions(Options programOptions, MailManager.MailManager mailManager)
{
    return new PriceCheckerOptions
    {
        LogDirectory = programOptions.LogFile is null ? null :
            Directory.Exists(programOptions.LogFile) ? programOptions.LogFile :
            Directory.GetParent(programOptions.LogFile)?.FullName,
        MailManager = mailManager,
        RunningOptions = PriceCheckerRunningOption.Asynchronously,
        EmailProviderSendingOptions = new EmailProviderSendingOptions
        {
            AttachCheckResult = true,
            EmailProviderSendingOptionsLogs = true,
            TargetEmail = programOptions.EmailManagerTargetEmail
        },
        WebDriverOptions = new WebDriverOptions
        {
            WebDriverType = programOptions.WebDriver ?? WebDriverType.Default
        },
        LoadingTimeout = TimeSpan.FromSeconds(programOptions.Timeout),
        FailureRetries = programOptions.Retries,
        WebsiteCredentialls = new WebsiteCredentialls
        {
            CeneoLogin = programOptions.CeneoLogin,
            CeneoPassword = programOptions.CeneoPassword
        },
        MakeScreenshotPastFailureLimit = programOptions.MakeScreenshotPastFailureLimit ?? false,
        PriceDifferencePercentage = programOptions.PriceDifferencePercentage
    };
}

var parserResult = Parser.Default.ParseArguments<Options>(args);
parserResult
    .WithParsed(Start)
    .WithNotParsed(error => Console.Error.WriteLine(string.Join(Environment.NewLine, error.ToString())));

EmailProvider DetectEmailProvider(string email)
{
    if (email.ToLower().EndsWith("gmail.com"))
        return EmailProvider.Gmail;
    if (email.ToLower().EndsWith("protonmail.com") || email.ToLower().EndsWith("proton.me"))
        return EmailProvider.ProtonMail;
    if (email.ToLower().EndsWith("hotmail.com") || email.ToLower().EndsWith("outlook.com"))
        return EmailProvider.Outlook;
    return EmailProvider.Unknown;
}

Logger CreateSerilogLogger(Options options)
{
    var logger = new LoggerConfiguration();
    logger.Enrich.FromLogContext();

    if (options.Verbosity.HasValue)
        logger.MinimumLevel.Is(options.Verbosity.Value);
    if (options.WriteToConsole)
        logger.WriteTo.Console(outputTemplate: options.LogTemplate);
    if (!string.IsNullOrEmpty(options.LogFile))
        logger.WriteTo.File(options.LogFile,
            rollingInterval: RollingInterval.Day,
            rollOnFileSizeLimit: true,
            fileSizeLimitBytes: 100 * 1024 * 1024,
            outputTemplate: options.LogTemplate);

    return logger.CreateLogger();
}

void Start(Options options)
{
    Console.WriteLine($"Launched program, set variables: {options}");
    var serilogLogger = CreateSerilogLogger(options);

    var mailManager = new MailManager.MailManager(DetectEmailProvider(options.EmailManagerSenderEmail),
        options.EmailManagerSenderEmail,
        options.EmailManagerSenderPassword);

    var checkerOptions = CreatePriceCheckerOptions(options, mailManager);
    var logger = LoggerFactory.Create(configuration =>
        {
            configuration.AddSerilog(serilogLogger);
            if (options.WriteToConsole)
                configuration.AddConsole();
        }
    ).CreateLogger("");
    logger.LogInformation("Started application");
    logger.LogInformation("Set options: {Options}", options);
    var checker = new PriceChecker(checkerOptions, logger);

    var timer = RunScheduler(checker, options);

    while (true)
    {
        Thread.Sleep(Timeout.Infinite);
        // Timer seems to be optimized and collected by GC if this line is missing after 1 hour??? HELLO??????
        var dummy = timer;
    }
    // ReSharper disable once FunctionNeverReturns
}

Timer RunScheduler(PriceChecker checker, Options options)
{
    var dueTime = options.StartDateTime is null ? TimeSpan.Zero : DateTime.Now - DateTime.Parse(options.StartDateTime);
    checker.Logger.LogInformation("Task is scheduled in {DueTime} minutes", dueTime.TotalMinutes.ToString());
    // ReSharper disable once AsyncVoidLambda
    return new Timer(async state =>
        {
            var stateAsTimer = (TimerState) state!;
            TryToSendOverdueErrors(stateAsTimer);
            try
            {
                stateAsTimer.PriceChecker.Logger.LogInformation(
                    "Starting executing scheduled task with repeating period of {Period} minutes",
                    options.Interval.ToString());
                await ExecuteAsync(stateAsTimer);
            }
            catch (Exception e)
            {
                stateAsTimer.PriceChecker.Logger.LogError(
                    "Unhandled error occurred during executing timer: {Exception}",
                    e.ToString());
                TryToSendException(stateAsTimer, e);
            }
            finally
            {
                stateAsTimer.PriceChecker.Logger.LogInformation("Finished executing scheduled task");
            }
        },
        new TimerState {Options = options, PriceChecker = checker},
        dueTime,
        TimeSpan.FromMinutes(options.Interval));
}

void TryToSendException(TimerState state, Exception e)
{
    try
    {
        state.PriceChecker.Options.MailManager.SendEmail(
            state.Options.EmailManagerTargetEmail,
            "Unhandled exception in executing timer",
            e.ToString());
    }
    catch (Exception exception)
    {
        state.OverdueNotSentErrors.Add(e.ToString());
        state.OverdueNotSentErrors.Add(exception.ToString());
    }
}

void TryToSendOverdueErrors(TimerState state)
{
    if (!state.OverdueNotSentErrors.Any())
        return;

    try
    {
        state.PriceChecker.Options.MailManager.SendEmail(
            state.Options.EmailManagerTargetEmail,
            "Overdue errors",
            $"There are {state.OverdueNotSentErrors.Count} overdue errors:{Environment.NewLine}"
            + string.Join(Environment.NewLine,
                state.OverdueNotSentErrors.Select((error, index) => $"===> Error {index}: {error}")));
        state.OverdueNotSentErrors.Clear();
    }
    catch (Exception e)
    {
        state.PriceChecker.Logger.LogError(
            "Failed to send overdue errors: {Exception}",
            e.ToString());
        state.OverdueNotSentErrors.Add(e.ToString());
    }
}

async Task ExecuteAsync(TimerState state)
{
    await state.PriceChecker.CheckAsync(state.Options.UrlsToCheck);
}