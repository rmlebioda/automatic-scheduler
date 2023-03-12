using System.Collections.ObjectModel;
using System.Globalization;
using CinemaCity.Models;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using WebDriverFramework;

namespace CinemaCity.Checkers;

public class CinemaCityChecker
{
    private const string SelectCinemaDataAutomationIdXPath = ".//div[@data-automation-id='selectCinema']";
    private const string SelectCinemaDataDropdownInnerXPath = ".//ul[@class='dropdown-menu inner']/li";

    private const string CinemaListResultXPath =
        ".//section[@class='qb-list-by-list']/div/div[contains(@class, 'movie-row')]";

    private const string CinemaNameElementXPath = ".//h4[contains(@class, 'qb-cinema-name')]";
    private readonly ILogger _logger;
    private readonly CinemaCityOptions _options;
    private readonly ScopedTask _scopedTask;
    private readonly IWebDriver _webDriver;
    private readonly By ByCinemaListResult = By.XPath(CinemaListResultXPath);
    private readonly By ByCinemaNameElement = By.XPath(CinemaNameElementXPath);
    private readonly By ByMovieInfoColumn = By.XPath(".//div[contains(@class, 'qb-movie-info-column')]");
    private readonly By BySelectCinemaDataAutomationIdValue = By.XPath(SelectCinemaDataAutomationIdXPath);
    private readonly By BySelectCinemaDataDropdownInner = By.XPath(SelectCinemaDataDropdownInnerXPath);

    public CinemaCityChecker(CinemaCityOptions options, IWebDriver webDriver, ILogger logger)
    {
        _options = options;
        _webDriver = webDriver;
        _logger = logger;
        _scopedTask = new ScopedTask(_logger,
            _options.FailureRetries,
            _options.MakeScreenshotAfterFailure ? _options.LogDirectory : null);
    }

    public CinemaCityCinemaReport Check(Uri uri)
    {
        try
        {
            GoToPage(uri);
            var selectCinemaElement = WaitForPageToLoadSelectCinema();
            ClickCookieConsents();
            Select(selectCinemaElement);
            var dropdownOptionsElement = WaitForPageToLoadOptions();
            var desiredDropdownOption = SelectDesiredDropdownOption(dropdownOptionsElement);
            Select(desiredDropdownOption);
            var cinemaLocationResults = WaitForCinemaListToLoad();
            var cinemaLocationResult = FilterCinemaResults(cinemaLocationResults);
            var showtimeWebElements = GetShowtimeWebElements(cinemaLocationResult);
            var showtimes = GetShowtimes(showtimeWebElements).ToList();
            return new CinemaCityCinemaReport(
                uri.AbsoluteUri,
                _options.CinemaName,
                _options.CinemaTownName,
                showtimes);
        }
        catch (Exception e)
        {
            _logger.LogError("Unexpected error: {Exception}", e);
            throw;
        }
    }

    private void GoToPage(Uri uri)
    {
        _scopedTask.Execute(() => _webDriver.Navigate().GoToUrl(uri),
            string.Format("Loading page {0}", uri.AbsoluteUri),
            _webDriver);
    }

    private IWebElement WaitForPageToLoadSelectCinema()
    {
        _logger.LogInformation("Waiting for page to load list of cinemas");
        return _webDriver.FindElement(BySelectCinemaDataAutomationIdValue, _options.LoadingTimeout);
    }

    private void ClickCookieConsents()
    {
        try
        {
            var consentElement = _webDriver.FindElement(By.Id("onetrust-accept-btn-handler"));
            consentElement.Click();
            Thread.Sleep(1000);
        }
        catch (Exception e)
        {
            // swallow errors
        }
    }

    private void Select(IWebElement element)
    {
        element.Click();
    }

    private ReadOnlyCollection<IWebElement> WaitForPageToLoadOptions()
    {
        _logger.LogInformation("Waiting for page to load list of cinema dropdown");
        var element = _webDriver.FindElement(BySelectCinemaDataDropdownInner, _options.LoadingTimeout);
        return _webDriver.FindElements(BySelectCinemaDataDropdownInner);
    }

    private IWebElement SelectDesiredDropdownOption(ReadOnlyCollection<IWebElement> dropdownOptionsElement)
    {
        try
        {
            _logger.LogInformation("Selecting desired cinema");
            return dropdownOptionsElement.First(element => element.Text.Contains(_options.CinemaTownName));
        }
        catch (Exception e)
        {
            _logger.LogError(
                "Failed to find Cinema town name: {CinemaTownName} in list of dropdowns (with count: {Count}",
                _options.CinemaTownName,
                dropdownOptionsElement.Count);
            throw;
        }
    }

    private ReadOnlyCollection<IWebElement> WaitForCinemaListToLoad()
    {
        _logger.LogInformation("Waiting for list of cinemas to load");
        var element = _webDriver.FindElement(ByCinemaListResult, _options.LoadingTimeout);
        return _webDriver.FindElements(ByCinemaListResult);
    }

    private IWebElement FilterCinemaResults(ReadOnlyCollection<IWebElement> cinemaLocationResults)
    {
        try
        {
            return cinemaLocationResults.First(element =>
                element.FindElement(ByCinemaNameElement).Text
                    .Contains(_options.CinemaName, StringComparison.CurrentCultureIgnoreCase));
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to find Cinema name: {CinemaName} in list of dropdowns (with count: {Count}",
                _options.CinemaName,
                cinemaLocationResults.Count);
            throw;
        }
    }

    private IEnumerable<IWebElement> GetShowtimeWebElements(IWebElement cinemaLocationResult)
    {
        var movieInfoColumnElements = cinemaLocationResult
            .FindElements(ByMovieInfoColumn);
        
        var selectionMovieTypes =
            movieInfoColumnElements.Where(element => _options.NeedsToBeVip == IsVipElement(element));

        var selectionMovieType = selectionMovieTypes.First();

        return selectionMovieType.FindElements(By.XPath(".//a[contains(@class, 'btn-lg')]"));

        bool IsVipElement(IWebElement element)
        {
            try
            {
                return element
                    .FindElements(By.XPath(".//ul[@class='qb-screening-attributes']//span"))
                    .Any(element => element.Text.ToLower() == "vip");
            }
            catch (NoSuchElementException e)
            {
                return false;
            }
        }
    }

    private IEnumerable<CinemaCityShowtime> GetShowtimes(IEnumerable<IWebElement> showtimeWebElements)
    {
        foreach (var showtimeWebElement in showtimeWebElements)
        {
            yield return CheckShowtime(showtimeWebElement);
        }
    }

    private CinemaCityShowtime CheckShowtime(IWebElement showtimeWebElement)
    {
        _webDriver.SwitchTo().Window(_webDriver.WindowHandles[0]);
        var showtimeDate =
            _webDriver.FindElement(By.XPath(".//div[contains(@class, 'qb-calendar-widget')]/div/div[2]/h5")).Text;
        var showtimeTime = showtimeWebElement.Text;
        var showtimeDateTime = DateTime.ParseExact(showtimeDate.Substring(showtimeDate.Length - 10) + " " + showtimeTime, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        ((IJavaScriptExecutor)_webDriver).ExecuteScript("arguments[0].scrollIntoView(true);", showtimeWebElement);
        ((IJavaScriptExecutor)_webDriver).ExecuteScript("scroll(0,-80)");
        showtimeWebElement.Click();
        var buyByGuestLocator = By.XPath("//a[@data-automation-id='guest-button']");
        var guestElement = _webDriver.FindElement(buyByGuestLocator, _options.LoadingTimeout);
        Thread.Sleep(100);
        var action = new Actions(_webDriver);
        action.KeyDown(Keys.LeftControl).Click(guestElement).KeyUp(Keys.LeftControl).Build().Perform();
        Thread.Sleep(100);
        var tabs = _webDriver.WindowHandles;
        _webDriver.SwitchTo().Window(tabs[1]);
        var showtime = BuildShowtimeOnPurchasePage(showtimeDateTime);
        _webDriver.Close();
        _webDriver.SwitchTo().Window(tabs[0]);
        return showtime;
    }

    private CinemaCityShowtime BuildShowtimeOnPurchasePage(DateTime showtimeDate)
    {
        _ = _webDriver.FindElement(By.Id("pagecontainer"), _options.LoadingTimeout);
        var errorPageWebElement = _webDriver.FindElements(By.XPath("//div[@class='errorpage']"));
        if (errorPageWebElement.Any())
        {
            return GetFailedShowtime();
        }

        return GetShowtime();

        CinemaCityShowtime GetFailedShowtime()
        {
            return new CinemaCityShowtime(
                showtimeDate,
                false,
                _webDriver.FindElement(By.XPath(".//table/tbody/tr[1]/td")).Text,
                _webDriver.FindElement(By.XPath(".//table/tbody/tr[2]/td")).Text,
                _webDriver.FindElement(By.XPath(".//table/tbody/tr[3]/td")).Text);
        }

        CinemaCityShowtime GetShowtime()
        {
            return new CinemaCityShowtime(
                showtimeDate,
                true);
        }
    }
}