using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using WebDriverFramework.Extensions;

namespace WebDriverFramework;

public class ScopedTask
{
    private readonly IList<ExceptionHandler> _customExceptionHandlers;
    private readonly ILogger _logger;
    private readonly int _retries;
    private readonly string? _screenshotDirectory;

    public ScopedTask(ILogger logger, int retries, string? screenshotDirectory = null,
        IEnumerable<ExceptionHandler>? customExceptionHandlers = null)
    {
        _logger = logger;
        _retries = retries;
        _screenshotDirectory = screenshotDirectory;
        _customExceptionHandlers = customExceptionHandlers?.ToList() ?? new List<ExceptionHandler>();
    }

    public T Execute<T>(Func<T> task, string scopedMessage, IWebDriver driver)
    {
        using (_logger.BeginScope(scopedMessage))
        {
            try
            {
                return TaskRepeater.Repeat(() => task(),
                    _retries,
                    (attempt, exception) =>
                    {
                        _logger.LogWarning(
                            string.Format("Attempt {0} failed due to exception: {1}",
                                attempt,
                                exception));
                    });
            }
            catch (Exception e)
            {
                var customExceptionHandler =
                    _customExceptionHandlers.FirstOrDefault(handler => handler.ShouldHandle(e));
                if (customExceptionHandler is not null)
                {
                    customExceptionHandler.Handler(e);
                    throw;
                }

                if (!string.IsNullOrEmpty(_screenshotDirectory))
                {
                    if (driver is ITakesScreenshot takesScreenshotDriver)
                    {
                        takesScreenshotDriver.TakeScreenshot(_screenshotDirectory!);
                    }
                    else
                    {
                        _logger.LogWarning(string.Format(
                            "Unable to make screenshot, because IDriver does not conform to type ITakesScreenshot (IDriver type is: {0}",
                            driver.GetType()));
                    }
                }

                _logger.LogError(string.Format("Failed to load page due to unhandled exception: {0}", e));
                throw;
            }
        }
    }

    public void Execute(Action task, string scopedMessage, IWebDriver driver)
    {
        using (_logger.BeginScope(scopedMessage))
        {
            try
            {
                TaskRepeater.Repeat(() => task(),
                    _retries,
                    (attempt, exception) =>
                    {
                        _logger.LogWarning(
                            string.Format("Attempt {0} failed due to exception: {1}", attempt, exception));
                    });
            }
            catch (Exception e)
            {
                var customExceptionHandler =
                    _customExceptionHandlers.FirstOrDefault(handler => handler.ShouldHandle(e));
                if (customExceptionHandler is not null)
                {
                    customExceptionHandler.Handler(e);
                    if (!customExceptionHandler.ShouldRetry)
                    {
                        throw;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(_screenshotDirectory))
                    {
                        if (driver is ITakesScreenshot takesScreenshotDriver)
                        {
                            takesScreenshotDriver.TakeScreenshot(_screenshotDirectory!);
                        }
                        else
                        {
                            _logger.LogWarning(string.Format(
                                "Unable to make screenshot, because IDriver does not conform to type ITakesScreenshot (IDriver type is: {0}",
                                driver.GetType()));
                        }
                    }

                    _logger.LogError(string.Format("Failed to load page due to unhandled exception: {0}",
                        e));
                    throw;
                }
            }
        }
    }
}
