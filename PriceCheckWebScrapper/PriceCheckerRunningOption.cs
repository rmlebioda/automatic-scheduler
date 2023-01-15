namespace PriceCheckWebScrapper;

public enum PriceCheckerRunningOption
{
    /// <summary>
    /// Executes every single url execution synchronously with only one web browser instance
    /// </summary>
    Synchronously = 0,
    
    /// <summary>
    /// Runs all urls from different hosts (domain) asynchronously with own web browser instance, urls with same domain are queued until previous execution is done
    /// </summary>
    AsynchronouslyPerDomain,
    
    /// <summary>
    /// Runs all urls asynchronously, each urls has it's own web browser instance
    /// </summary>
    Asynchronously
}