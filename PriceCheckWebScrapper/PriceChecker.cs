using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using PriceCheckWebScrapper.Core;
using PriceCheckWebScrapper.Core.Mail;
using PriceCheckWebScrapper.Resolvers;

namespace PriceCheckWebScrapper;

/// <summary>
///     Global class for checking prices of given urls with desired settings and notifications.
/// </summary>
public class PriceChecker
{
    private readonly Dictionary<Uri, ProductPriceOffersReport> _lastSentUrlsReports = new();
    private readonly Dictionary<string, IWebDriver> _webDrivers = new();
    public readonly PriceCheckerOptions Options;
    public readonly ILogger Logger;

    public PriceChecker(PriceCheckerOptions options, ILogger logger)
    {
        Options = options;
        Logger = logger;
    }

    /// <summary>
    ///     Execute price check for given URL's.
    /// </summary>
    /// <param name="urls">URL's to be checked</param>
    /// <param name="overrideOptions">Settings, that overrides base settings set in constructor</param>
    public async Task CheckAsync(IEnumerable<string> urls, PriceCheckerOptions? overrideOptions = null)
    {
        using var scope = Logger.BeginScope("Started checking execution");
        var options = overrideOptions ?? Options;

        var uris = urls.Select(url => new Uri(url));
        switch (Options.RunningOptions)
        {
            case PriceCheckerRunningOption.Synchronously:
            {
                var driver = GetWebDriver(options.WebDriverOptions);
                var reports = new List<ProductPriceOffersReport>();
                foreach (var uri in uris)
                    reports.Add(CheckUri(driver, uri));
                Report(reports);
                break;
            }
            case PriceCheckerRunningOption.AsynchronouslyPerDomain:
            {
                var urisByDomain = uris.GroupBy(uri => uri.Host);
                var tasks = urisByDomain.Select(domainUris => new Task<IEnumerable<ProductPriceOffersReport>>(() =>
                {
                    var driver = GetWebDriver(domainUris.Key, options.WebDriverOptions);
                    return GetReports();

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
                    var driver = CreateNewWebDriver(options.WebDriverOptions);
                    var result = CheckUri(driver, uri);
                    driver.Close();
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

        var newWebDriver = CreateNewWebDriver(webDriverOptions);
        _webDrivers.Add(domain, newWebDriver);
        return newWebDriver;
    }

    private IWebDriver GetWebDriver(WebDriverOptions webDriverOptions)
    {
        if (_webDrivers.Count > 0)
            return _webDrivers.Values.First();

        var newWebDriver = CreateNewWebDriver(webDriverOptions);
        _webDrivers.Add(string.Empty, newWebDriver);
        return newWebDriver;
    }

    private static IWebDriver CreateNewWebDriver(WebDriverOptions webDriverOptions)
    {
        switch (webDriverOptions.WebDriverType)
        {
            case WebDriverType.Chrome:
            {
                var webDriver = new ChromeDriver();
                return webDriver;
            }
            case WebDriverType.Default:
            case WebDriverType.Firefox:
            {
                var options = new FirefoxOptions();
                options.AddArgument("-headless");
                var webDriver = new FirefoxDriver(options);
                return webDriver;
            }
            default:
                throw new ArgumentException("Invalid web driver: " + webDriverOptions.WebDriverType);
        }
    }

    private void Report(IEnumerable<ProductPriceOffersReport> productsPriceOffersReports)
    {
        Logger.LogInformation("Sending built reports of products");
        var reportsToSend = new List<ProductPriceOffersReport>();

        foreach (var report in productsPriceOffersReports)
        {
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
                builder.TargetEmail, title, body);
            Options.MailManager.SendEmail(builder.TargetEmail, title, body);
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