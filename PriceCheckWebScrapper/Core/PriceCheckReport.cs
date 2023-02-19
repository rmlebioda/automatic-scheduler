namespace PriceCheckWebScrapper.Core;

[Obsolete("Useless???????????????")]
public readonly record struct PriceCheckReport(IEnumerable<ProductPriceOffersReport> PriceCheckInstances)
{
    public static PriceCheckReport Empty => new PriceCheckReport(Enumerable.Empty<ProductPriceOffersReport>());
}
