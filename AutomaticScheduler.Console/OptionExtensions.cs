using CinemaCity;
using MailManager;
using PriceCheckWebScrapper;
using Serilog;
using Serilog.Core;
using WebDriverFramework;

namespace AutomaticScheduler.Console;

public static class OptionExtensions
{
    public static PriceCheckerOptions CreatePriceCheckerOptions(this Options programOptions,
        MailManager.MailManager mailManager)
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
            MakeScreenshotAfterFailure = programOptions.MakeScreenshotAfterFailure ?? false,
            PriceDifferencePercentage = programOptions.PriceDifferencePercentage
        };
    }

    public static Logger CreateSerilogLogger(this Options options)
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

    public static EmailProvider GetEmailProvider(this Options options)
    {
        if (options.EmailManagerSenderEmail.ToLower().EndsWith("gmail.com"))
            return EmailProvider.Gmail;
        if (options.EmailManagerSenderEmail.ToLower().EndsWith("protonmail.com") ||
            options.EmailManagerSenderEmail.ToLower().EndsWith("proton.me"))
            return EmailProvider.ProtonMail;
        if (options.EmailManagerSenderEmail.ToLower().EndsWith("hotmail.com") ||
            options.EmailManagerSenderEmail.ToLower().EndsWith("outlook.com"))
            return EmailProvider.Outlook;
        return EmailProvider.Unknown;
    }

    public static CinemaCityOptions CreateCinemaCityOptions(this Options programOptions,
        MailManager.MailManager mailManager)
    {
        return new CinemaCityOptions
        {
            LogDirectory = programOptions.LogFile is null ? null :
                Directory.Exists(programOptions.LogFile) ? programOptions.LogFile :
                Directory.GetParent(programOptions.LogFile)?.FullName,
            MailManager = mailManager,
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
            MakeScreenshotAfterFailure = programOptions.MakeScreenshotAfterFailure ?? false,
            CinemaName =
                programOptions.CinemaCityName ?? throw new ArgumentException("Cinema city name cannot be null"),
            CinemaTownName = programOptions.CinemaCityTownName ??
                             throw new ArgumentException("Cinema city location name cannot be null"),
            NeedsToBeVip = programOptions.CinemaCityVip ??
                             throw new ArgumentException("Cinema VIP name cannot be null")
        };
    }
}