using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using PriceCheckWebScrapper.Checkers.Ceneo.Models;
using PriceCheckWebScrapper.Core;
using PriceCheckWebScrapper.Exceptions;
using WebDriverFramework;
using WebDriverFramework.Extensions;

namespace PriceCheckWebScrapper.Checkers.Ceneo;

internal class CeneoPriceChecker : IPriceChecker
{
    private const string ProductNameClassName = "product-top__product-info__name";
    private readonly List<ExceptionHandler> _exceptionHandlers;
    private readonly bool _isLoggingInRequired = false;
    private readonly ILogger _logger;
    private readonly PriceCheckerOptions _priceCheckerOptions;
    private readonly ScopedTask _scopedTask;

    public CeneoPriceChecker(PriceCheckerOptions priceCheckerOptions, ILogger logger)
    {
        _priceCheckerOptions = priceCheckerOptions;
        _logger = logger;
        _exceptionHandlers = new List<ExceptionHandler> {PriceExceptionHandler};
        _scopedTask = new ScopedTask(_logger,
            _priceCheckerOptions.FailureRetries,
            _priceCheckerOptions.MakeScreenshotAfterFailure ? _priceCheckerOptions.LogDirectory : null,
            _exceptionHandlers);
    }

    private By ByProductNameClassName => By.ClassName(ProductNameClassName);

    private ExceptionHandler PriceExceptionHandler =>
        new()
        {
            ShouldRetry = false,
            ShouldHandle = exception =>
                exception is PriceCheckException && ((PriceCheckException) exception).IsExceptionReasonFatal,
            Handler = exception =>
                _logger.LogError(string.Format("Execution halted, encountered unrecoverable exception: {0}",
                    exception))
        };

    public ProductPriceOffersReport CheckPrice(IWebDriver webDriver, Uri uri)
    {
        try
        {
            using (_logger.BeginScope(string.Format("Checking price on url {0}", uri.AbsoluteUri)))
            {
                LoadWebpage(webDriver, uri);
                if (_isLoggingInRequired)
                    LogInUser(webDriver, uri);
                return CheckProductPrice(webDriver, uri);
            }
        }
        finally
        {
            _logger.LogInformation("Finished execution of checking price");
        }
    }

    private void LoadWebpage(IWebDriver webDriver, Uri uri)
    {
        _scopedTask.Execute(() => { webDriver.Navigate().GoToUrl(uri.AbsoluteUri); },
            string.Format("Loading page {0}", uri.AbsoluteUri),
            webDriver);
    }

    private bool IsLoggedIn(IWebDriver webDriver)
    {
        // verify host website, we are on
        const string loggedInElement =
            "//header//div[contains(@class, 'header__user')]//div[contains(@class, 'header__user__auth__avatar')]";
        var authAvatar = webDriver.FindElement(By.XPath(loggedInElement));
        _logger.LogDebug(string.Format("Auth avatar was {0}found.", authAvatar is null ? "not " : string.Empty));
        return authAvatar != null;
    }

    private void LogInUser(IWebDriver webDriver, Uri uri)
    {
        _scopedTask.Execute(() =>
            {
                if (!IsLoggedIn(webDriver))
                {
                    _logger.LogInformation("User is not logged in, logging in with credentials");

                    ClickLogIn();
                    var logRegForm = FindLogRegForm();
                    SubmitForm(logRegForm);
                    ValidateLogIn();
                }
            },
            "Logging in",
            webDriver);

        void ClickLogIn()
        {
            _logger.LogTrace("Finding log in span by xpath");
            const string userLogInSpanXPath = "//header//div[@class='my-account']//span[@class='header__user__icon']";
            var byUserLogInSpan = By.XPath(userLogInSpanXPath);
            var userLogInSpan = webDriver.FindElement(byUserLogInSpan);
            if (userLogInSpan is null)
                throw PriceCheckException.FromBy(byUserLogInSpan);

            _logger.LogDebug("Clicking on user log in span");
            userLogInSpan.Click();
        }

        IWebElement FindLogRegForm()
        {
            _logger.LogTrace("Finding log in and registry form by xpath");
            const string logRegFormXPath = "//form[contains(@class, 'log-reg-form')]";
            var byLogRegForm = By.XPath(logRegFormXPath);
            var logRegForm = webDriver.FindElement(byLogRegForm);
            if (logRegForm is null)
                throw PriceCheckException.FromBy(byLogRegForm);
            return logRegForm;
        }

        void SubmitForm(IWebElement logRegForm)
        {
            _logger.LogDebug("Filling log in form with credentials");
            const string formRelativeLoginXPath = "//input[@name='Login']";
            const string formRelativePasswordXPath = "//input[@name='password']";
            var byFormRelativeLoginXPath = By.XPath(formRelativeLoginXPath);
            var byFormRelativePasswordXPath = By.XPath(formRelativePasswordXPath);
            var loginInput = logRegForm.FindElement(byFormRelativeLoginXPath);
            if (loginInput is null)
                throw PriceCheckException.FromBy(byFormRelativeLoginXPath);
            var passwordInput = logRegForm.FindElement(byFormRelativePasswordXPath);
            if (passwordInput is null)
                throw PriceCheckException.FromBy(byFormRelativePasswordXPath);
            loginInput.SendKeys(_priceCheckerOptions.WebsiteCredentialls.CeneoLogin ?? throw new PriceCheckException(
                PriceCheckExceptionType.MissingCredentials,
                "Unable to log in to ceneo.pl, because login was not provided"));
            passwordInput.SendKeys(_priceCheckerOptions.WebsiteCredentialls.CeneoPassword ??
                                   throw new PriceCheckException(PriceCheckExceptionType.MissingCredentials,
                                       "Unable to log in to ceneo.pl, because password was not provided"));

            _logger.LogInformation("Credentials filled, logging in");
            const string formRelativeSubmitXPath = "//input[@type='submit']";
            var byFormRelativeSubmit = By.XPath(formRelativeSubmitXPath);
            var formSubmit = logRegForm.FindElement(byFormRelativeSubmit);
            if (formSubmit is null)
                throw PriceCheckException.FromBy(byFormRelativeSubmit);
            formSubmit.Click();
        }

        void ValidateLogIn()
        {
            CheckForErrors();
            EnsureRightPageIsLoaded(webDriver, uri);
            CheckIfUserIsLoggedIn();

            void CheckForErrors()
            {
                // validate, if logged in successfully and exit, otherwise raise exception and repeat if credentials were not invalid
                var errorAlertXPath = "//h3[contains(@class, 'alert-error')]";
                var byErrorAlert = By.XPath(errorAlertXPath);
                var alerts = webDriver.FindElements(byErrorAlert);
                if (alerts.Any())
                    throw new PriceCheckException(PriceCheckExceptionType.InvalidCredentials,
                        string.Format("Failed to login in due to errors: {0}", alerts.Select(alert => alert.Text)));
            }

            void CheckIfUserIsLoggedIn()
            {
                if (!IsLoggedIn(webDriver))
                {
                    if (!string.IsNullOrEmpty(_priceCheckerOptions.LogDirectory) &&
                        webDriver is ITakesScreenshot takesScreenshot)
                        takesScreenshot.TakeScreenshot(_priceCheckerOptions.LogDirectory);

                    throw new PriceCheckException(PriceCheckExceptionType.OtherLogInFailure,
                        "Failed to log in due to unknown error, after full execution of log in operation, user is still not logged in and error was not detected properly");
                }
            }
        }
    }

    private void EnsureRightPageIsLoaded(IWebDriver webDriver, Uri uri)
    {
        _logger.LogDebug(string.Format("Ensuring, that current webpage {0} matches desired Uri {1}",
            webDriver.Url,
            uri.AbsoluteUri));
        if (webDriver.Url != uri.AbsoluteUri)
        {
            _logger.LogWarning(string.Format(
                "Errors were not detected, but browser was redirected to webpage {0}, even though it was expected to stay on {1}, attempting to make screenshot",
                webDriver.Url,
                uri.AbsoluteUri));
            if (!string.IsNullOrEmpty(_priceCheckerOptions.LogDirectory) &&
                webDriver is ITakesScreenshot takesScreenshot)
                takesScreenshot.TakeScreenshot(_priceCheckerOptions.LogDirectory);

            webDriver.Navigate().GoToUrl(uri.AbsoluteUri);
        }
    }

    public string GetProductName(IWebDriver webDriver)
    {
        return webDriver.FindElement(ByProductNameClassName).Text;
    }

    private ProductPriceOffersReport CheckProductPrice(IWebDriver webDriver, Uri uri)
    {
        return _scopedTask.Execute(() =>
            {
                EnsureRightPageIsLoaded(webDriver, uri);
                var productsOffers = GetMultipleProductOffers();
                return ReportProductsOffers(productsOffers);
            },
            "Checking price",
            webDriver);

        IEnumerable<ProductOffers> GetMultipleProductOffers()
        {
            const string productOffersClassName = "product-offers";
            _logger.LogInformation("Parsing product offers");
            var productsOffersElements = webDriver
                .FindElements(By.ClassName(productOffersClassName));
            var productsOffers = productsOffersElements
                .Select(element => new ProductOffers(element));
            return productsOffers;
        }


        ProductPriceOffersReport ReportProductsOffers(IEnumerable<ProductOffers> multipleProductOffers)
        {
            _logger.LogInformation("Building report");
            var listMultipleProductOffers = multipleProductOffers.ToList();
            if (!listMultipleProductOffers.Any())
                return ProductPriceOffersReport.Empty(uri, uri.AbsoluteUri);

            var reportBuilder = new ProductPriceOffersReportBuilder(uri);

            foreach (var productOffers in listMultipleProductOffers)
            {
                var offers = productOffers
                    .GetProductOffers();
                var newReport = GetProductOffersReport(productOffers, offers);
                reportBuilder.AddReport(newReport);
            }

            return reportBuilder.Build();

            ProductPriceOffersReport GetProductOffersReport(ProductOffers productOffers,
                IEnumerable<ProductOffer> offers)
            {
                var orderedOffers = offers.OrderBy(offer => offer.Price).ToList();
                if (!orderedOffers.Any())
                {
                    _logger.LogWarning("No offers available");
                    return ProductPriceOffersReport.Empty(uri, GetProductName(webDriver));
                }

                var reportOffers = new List<SingularProductPriceOffer>();
                foreach (var offer in orderedOffers)
                    reportOffers.Add(offer.GetSingularProductPriceOffer());

                return new ProductPriceOffersReport(uri, GetProductName(webDriver), reportOffers);
            }
        }
    }
}