namespace WebDriverFramework;

public record ExceptionHandler
{
    public Func<Exception, bool> ShouldHandle { get; init; }
    public bool ShouldRetry { get; init; }
    public Action<Exception> Handler { get; init; }
}