using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using PriceCheckWebScrapper.Core;
using PriceCheckWebScrapper.Core.Mail;
using PriceCheckWebScrapper.Exceptions;
using PriceCheckWebScrapper.Resolvers;
using WebDriverFramework;

namespace PriceCheckWebScrapper;

/// <summary>
///     Global class for checking prices of given urls with desired settings and notifications.
/// </summary>
public class PriceChecker
{
    private readonly Dictionary<Uri, ProductPriceOffersReport> _lastSentUrlsReports = new();
    private readonly Dictionary<string, IWebDriver> _webDrivers = new();
    public readonly ILogger Logger;
    public readonly PriceCheckerOptions Options;

    public PriceChecker(PriceCheckerOptions options, ILogger logger)
    {
        Options = options;
        Logger = logger;
    }

    /// <summary>
    /// Checks if URI is supported by this class
    /// </summary>
    /// <param name="uri">URI to check</param>
    /// <returns>if URI is supported</returns>
    public static bool IsSupported(Uri uri) => PriceCheckerResolver.IsSupported(uri);

    /// <summary>
    ///     Execute price check for given URL's.
    /// </summary>
    /// <param name="urls">URL's to be checked</param>
    /// <param name="overrideOptions">Settings, that overrides base settings set in constructor</param>
    public async Task CheckAsync(IEnumerable<Uri> uris, PriceCheckerOptions? overrideOptions = null)
    {
        using var scope = Logger.BeginScope("Started checking execution");
        var options = overrideOptions ?? Options;

        switch (Options.RunningOptions)
        {
            case PriceCheckerRunningOption.Synchronously:
            {
                var reports = new List<ProductPriceOffersReport>();
                var driver = GetWebDriver(options.WebDriverOptions);
                try
                {
                    foreach (var uri in uris)
                        reports.Add(CheckUri(driver, uri));
                }
                finally
                {
                    driver.Close();
                }

                Report(reports);
                break;
            }
            case PriceCheckerRunningOption.AsynchronouslyPerDomain:
            {
                var urisByDomain = uris.GroupBy(uri => uri.Host);
                var tasks = urisByDomain.Select(domainUris => new Task<IEnumerable<ProductPriceOffersReport>>(() =>
                {
                    var driver = GetWebDriver(domainUris.Key, options.WebDriverOptions);
                    try
                    {
                        return GetReports();
                    }
                    finally
                    {
                        driver.Close();
                    }

                    IEnumerable<ProductPriceOffersReport> GetReports()
                    {
                        foreach (var uri in domainUris)
                            yield return CheckUri(driver, uri);
                    }
                })).ToList();
                tasks.ForEach(task => task.Start());
                var reports = await Task.WhenAll(tasks);
                Report(reports.SelectMany(x => x));
                break;
            }
            case PriceCheckerRunningOption.Asynchronously:
            {
                var tasks = uris.Select(uri => new Task<ProductPriceOffersReport>(() =>
                {
                    var driver = WebDriverManager.CreateNewWebDriver(options.WebDriverOptions);
                    ProductPriceOffersReport result;
                    try
                    {
                        result = CheckUri(driver, uri);
                    }
                    finally
                    {
                        driver.Close();
                    }

                    return result;
                })).ToList();
                tasks.ForEach(task => task.Start());
                var reports = await Task.WhenAll(tasks);
                Report(reports);
                break;
            }
            default:
                throw new ArgumentException(string.Format("Invalid checker option: {0}", Options.RunningOptions));
        }
    }

    private ProductPriceOffersReport CheckUri(IWebDriver webDriver, Uri uri)
    {
        return PriceCheckerResolver.Resolve(uri, Options, Logger).CheckPrice(webDriver, uri);
    }

    private IWebDriver GetWebDriver(string domain, WebDriverOptions webDriverOptions)
    {
        if (_webDrivers.ContainsKey(domain))
            return _webDrivers[domain];

        var newWebDriver = WebDriverManager.CreateNewWebDriver(webDriverOptions);
        _webDrivers.Add(domain, newWebDriver);
        return newWebDriver;
    }

    private IWebDriver GetWebDriver(WebDriverOptions webDriverOptions)
    {
        if (_webDrivers.Count > 0)
            return _webDrivers.Values.First();

        var newWebDriver = WebDriverManager.CreateNewWebDriver(webDriverOptions);
        _webDrivers.Add(string.Empty, newWebDriver);
        return newWebDriver;
    }

    private void Report(IEnumerable<ProductPriceOffersReport> productsPriceOffersReports)
    {
        Logger.LogInformation("Sending built reports of products");
        var reportsToSend = new List<ProductPriceOffersReport>();

        foreach (var report in productsPriceOffersReports)
            if (DoesQualifyForReporting(report))
            {
                Logger.LogInformation("Report {Report} is qualifying for sending via email and will be sent shortly",
                    report.ToString());
                reportsToSend.Add(report);
            }
            else
            {
                Logger.LogInformation("Report {Report} does not qualify for sending via email", report.ToString());
            }

        if (reportsToSend.Any())
        {
            SendReports(reportsToSend);
            UpdateSentReports(reportsToSend);
        }

        bool DoesQualifyForReporting(ProductPriceOffersReport report)
        {
            if (!_lastSentUrlsReports.ContainsKey(report.Uri))
                return true;

            return _lastSentUrlsReports[report.Uri].DoesQualifyForReporting(report, Options.PriceDifferencePercentage);
        }

        void SendReports(IEnumerable<ProductPriceOffersReport> reports)
        {
            var builder = new MailBuilder(Options.EmailProviderSendingOptions, reports);
            var title = builder.Title;
            var body = builder.GetBody();
            Logger.LogInformation("Sending email to {TargetEmail} with title {Title} and body {Body}",
                builder.TargetEmail,
                title,
                body);
            try
            {
                Options.MailManager.SendEmail(builder.TargetEmail, title, body);
            }
            catch (Exception e)
            {
                throw new EmailSendReportException("Failed to send email due to exception: " + e.Message, e);
            }
        }

        void UpdateSentReports(List<ProductPriceOffersReport> productPriceOffersReports)
        {
            foreach (var report in productPriceOffersReports)
            {
                if (!_lastSentUrlsReports.ContainsKey(report.Uri))
                    _lastSentUrlsReports.Add(report.Uri, report);

                _lastSentUrlsReports[report.Uri] = report;
            }
        }
    }
}