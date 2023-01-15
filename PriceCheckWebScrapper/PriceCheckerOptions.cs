using MailManager;

namespace PriceCheckWebScrapper;

public class PriceCheckerOptions
{
    /// <summary>
    /// Email manager responsible for sending check or error results
    /// </summary>
    public MailManager.MailManager MailManager { get; set; }
    
    /// <summary>
    /// Sets what should be attached with email provider
    /// </summary>
    public EmailProviderSendingOptions? EmailProviderSendingOptions { get; set; }

    /// <summary>
    /// How script should be ran
    /// </summary>
    public PriceCheckerRunningOption RunningOptions { get; set; } = PriceCheckerRunningOption.AsynchronouslyPerDomain;
    
    /// <summary>
    /// Desired web driver
    /// </summary>
    public WebDriverOptions WebDriverOptions { get; set; }
}