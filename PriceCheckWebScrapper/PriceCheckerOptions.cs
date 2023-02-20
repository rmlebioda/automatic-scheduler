namespace PriceCheckWebScrapper;

public readonly record struct PriceCheckerOptions()
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
    /// How script should be ran
    /// </summary>
    public PriceCheckerRunningOption RunningOptions { get; init; } = PriceCheckerRunningOption.AsynchronouslyPerDomain;
    
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
    public bool MakeScreenshotPastFailureLimit { get; init; }
    
    /// <summary>
    /// Website login credentials
    /// </summary>
    public WebsiteCredentialls WebsiteCredentialls { get; init; }
    
    /// <summary>
    /// Price difference in percentage, to which change of best price is considered as worthy of sending email
    /// </summary>
    public double PriceDifferencePercentage { get; init; }
}