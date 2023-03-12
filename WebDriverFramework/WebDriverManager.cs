using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;

namespace WebDriverFramework;

public static class WebDriverManager
{
    public static IWebDriver CreateNewWebDriver(WebDriverOptions webDriverOptions)
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
#if !DEBUG
                options.AddArgument("-headless");
#endif
                var webDriver = new FirefoxDriver(options);
                return webDriver;
            }
            default:
                throw new ArgumentException("Invalid web driver: " + webDriverOptions.WebDriverType);
        }
    }

    public static IWebElement FindElement(this IWebDriver driver, By by, TimeSpan timeSpan)
    {
        if (timeSpan.TotalMilliseconds > 0)
        {
            var wait = new WebDriverWait(driver, timeSpan);
            return wait.Until(drv => drv.FindElement(by));
        }

        return driver.FindElement(by);
    }
}