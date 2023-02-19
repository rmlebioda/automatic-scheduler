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
        MakeScreenshotPastFailureLimit = programOptions.MakeScreenshotPastFailureLimit ?? false
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

    if (options.Verbosity.HasValue)
        logger.MinimumLevel.Is(options.Verbosity.Value);
    if (options.WriteToConsole)
        logger.WriteTo.Console();
    if (!string.IsNullOrEmpty(options.LogFile))
        logger.WriteTo.File(options.LogFile,
            rollingInterval: RollingInterval.Day,
            rollOnFileSizeLimit: true,
            fileSizeLimitBytes: 100 * 1024 * 1024);

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
    var checker = new PriceChecker(checkerOptions, logger);

    var timer = RunScheduler(checker, options);

    while (true) Thread.Sleep(Timeout.Infinite);
}

Timer RunScheduler(PriceChecker checker, Options options)
{
    // ReSharper disable once AsyncVoidLambda
    return new Timer(async state =>
        {
            var stateAsTimer = ((TimerState)state!);
            try
            {
                await ExecuteAsync(stateAsTimer);
            }
            catch (Exception e)
            {
                stateAsTimer.PriceChecker.Logger.LogError(string.Format("Unhandled error occurred during executing timer: {0}", e.ToString()));
                throw;
            }
        },
        new TimerState { Options = options, PriceChecker = checker },
        options.StartDateTime is null ? TimeSpan.Zero : DateTime.Now - DateTime.Parse(options.StartDateTime),
        TimeSpan.FromMinutes(options.Interval));
}

async Task ExecuteAsync(TimerState state)
{
    await state.PriceChecker.CheckAsync(state.Options.UrlsToCheck);
}