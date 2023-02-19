namespace PriceCheckWebScrapper;

public readonly record struct WebsiteCredentialls
{
    public string? CeneoLogin { get; init; }
    public string? CeneoPassword { get; init; }
}