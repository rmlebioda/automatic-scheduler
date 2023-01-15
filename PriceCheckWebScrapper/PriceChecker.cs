using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using PriceCheckWebScrapper.Resolvers;

namespace PriceCheckWebScrapper;

/// <summary>
/// Global class for checking prices of given urls with desired settings and notifications.
/// </summary>
public class PriceChecker
{
    private readonly PriceCheckerOptions _options;
    private readonly ILogger _logger;
    private readonly Dictionary<string, IWebDriver> _webDrivers = new Dictionary<string, IWebDriver>();

    public PriceChecker(PriceCheckerOptions options, ILogger logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Execute price check for given URL's.
    /// </summary>
    /// <param name="urls">URL's to be checked</param>
    /// <param name="overrideOptions">Settings, that overrides base settings set in constructor</param>
    public async Task CheckAsync(IEnumerable<string> urls, PriceCheckerOptions overrideOptions = null)
    {
        var uris = urls.Select(url => new Uri(url));
        switch (_options.RunningOptions)
        {
            case PriceCheckerRunningOption.Synchronously:
            {
                var driver = GetWebDriver();
                break;
            }
            case PriceCheckerRunningOption.AsynchronouslyPerDomain:
            {
                break;
            }
            case PriceCheckerRunningOption.Asynchronously:
            {
                break;
            }
            default:
                throw new ArgumentException("Invalid checker option");
        }
    }

    private async void CheckUri(IWebDriver webDriver, Uri uri)
    {
    }

    private IWebDriver GetWebDriver()
    {
        if (_webDrivers.Count > 0)
        {
            return _webDrivers.Values.First();
        }

        var newWebDriver = CreateNewWebDriver();
        _webDrivers.Add(string.Empty, newWebDriver);
        return newWebDriver;
    }

    private IWebDriver CreateNewWebDriver()
    {
    }
}