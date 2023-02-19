namespace PriceCheckWebScrapper;

public readonly record struct EmailProviderSendingOptions()
{
    public bool EmailProviderSendingOptionsLogs { get; init; }
    public bool AttachCheckResult { get; init; }
    public string TargetEmail { get; init; }
}