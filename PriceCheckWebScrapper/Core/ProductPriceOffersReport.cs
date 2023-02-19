namespace PriceCheckWebScrapper.Core;

public readonly record struct ProductPriceOffersReport(Uri Uri, string ProductName, IEnumerable<SingularProductPriceOffer> ProductPriceOffers)
{
    public static ProductPriceOffersReport Empty(Uri uri, string productName) => new ProductPriceOffersReport(uri, productName, Enumerable.Empty<SingularProductPriceOffer>());

    public bool DoesQualifyForReporting(ProductPriceOffersReport otherReport)
    {
        var thisBestOffer = ProductPriceOffers.MinBy(offer => offer.Price);
        var otherBestOffer = otherReport.ProductPriceOffers.MinBy(offer => offer.Price);
        if (Math.Abs(thisBestOffer.Price - otherBestOffer.Price) > thisBestOffer.Price / 100)
            return true;
        return false;
    }
}

public readonly record struct SingularProductPriceOffer(decimal Price, string ShopAddressName);