using PriceCheckWebScrapper;
using WebDriverFramework;

namespace CinemaCity;

public readonly record struct CinemaCityOptions()
{
    /// <summary>
    /// Directory for storing additional logs/screenshots
    /// </summary>
    public string? LogDirectory { get; init; }
    
    /// <summary>
    /// Email manager responsible for sending check or error results
    /// </summary>
    public MailManager.MailManager MailManager { get; init; }
    
    /// <summary>
    /// Sets what should be attached with email provider
    /// </summary>
    public EmailProviderSendingOptions EmailProviderSendingOptions { get; init; }
    
    /// <summary>
    /// Desired web driver
    /// </summary>
    public WebDriverOptions WebDriverOptions { get; init; }
    
    /// <summary>
    /// Webpage and operations loading timeout
    /// </summary>
    public TimeSpan LoadingTimeout { get; init; }
    
    /// <summary>
    /// How many times execution should be retried in case of unexpected failure
    /// </summary>
    public int FailureRetries { get; init; }
    
    /// <summary>
    /// Whenever screenshot should be made after failure of executing task with given <see cref="FailureRetries"/>
    /// </summary>
    public bool MakeScreenshotAfterFailure { get; init; }
    
    /// <summary>
    /// Town, in which desired cinema is located
    /// </summary>
    public string CinemaTownName { get; init; }
    
    /// <summary>
    /// Cinema name to search for
    /// </summary>
    public string CinemaName { get; init; }
    
    /// <summary>
    /// If VIP is required
    /// </summary>
    public bool NeedsToBeVip { get; init; }
}
