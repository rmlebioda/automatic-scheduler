using AutomaticScheduler.Console;
using CommandLine;
using MailManager;
using Microsoft.Extensions.Logging;
using PriceCheckWebScrapper;
using Serilog;
using Serilog.Core;

Parser.Default.ParseArguments<Options>(args)
    .WithParsedAsync(async options => await Start(options));

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

Logger CreateLogger(Options options)
{
    var logger = new LoggerConfiguration();

    if (options.Verbosity.HasValue)
        logger.MinimumLevel.Is(options.Verbosity.Value);
    if (options.WriteToConsole == true)
        logger.WriteTo.Console();
    if (!string.IsNullOrEmpty(options.LogFile))
        logger.WriteTo.File(options.LogFile,
            rollingInterval: RollingInterval.Day,
            rollOnFileSizeLimit: true,
            fileSizeLimitBytes: 100 * 1024 * 1024);

    return logger.CreateLogger();
}

async Task Start(Options options)
{
    var logger = CreateLogger(options);

    var mailManager = new MailManager.MailManager(DetectEmailProvider(options.EmailManagerSenderEmail),
        options.EmailManagerSenderEmail,
        options.EmailManagerSenderPassword);

    var checkerOptions = new PriceCheckerOptions
    {
        MailManager = mailManager,
        RunningOptions = PriceCheckerRunningOption.Asynchronously,
        EmailProviderSendingOptions = new EmailProviderSendingOptions
        {
            AttachCheckResult = true,
            EmailProviderSendingOptionsLogs = true
        },
        WebDriverOptions = new WebDriverOptions
        {
            WebDriverType = options.WebDriver ?? WebDriverType.Chrome
        }
    };
    var ilogger = LoggerFactory.Create(configuration => configuration.AddSerilog(logger)).CreateLogger("");
    var checker = new PriceChecker(checkerOptions, ilogger);

    var timer = RunScheduler(checker, options);
    
    while (true)
    {
        Thread.Sleep(Timeout.Infinite);
    }
}

Timer RunScheduler(PriceChecker checker, Options options)
{
    // ReSharper disable once AsyncVoidLambda
    return new Timer(async state =>
        {
            try
            {
                await ExecuteAsync((TimerState) state!);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        },
        new TimerState {Options = options, PriceChecker = checker},
        DateTime.Now - DateTime.Parse(options.StartDateTime),
        TimeSpan.FromMinutes(options.Interval));
}

async Task ExecuteAsync(TimerState state)
{
    await state.PriceChecker.CheckAsync(state.Options.UrlsToCheck);
}