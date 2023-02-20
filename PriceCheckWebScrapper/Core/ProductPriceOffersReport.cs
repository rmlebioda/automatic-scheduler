using System.Text.Json;

namespace PriceCheckWebScrapper.Core;

public readonly record struct ProductPriceOffersReport(Uri Uri,
    string ProductName,
    IEnumerable<SingularProductPriceOffer> ProductPriceOffers)
{
    public static ProductPriceOffersReport Empty(Uri uri, string productName)
    {
        return new(uri, productName, Enumerable.Empty<SingularProductPriceOffer>());
    }

    public bool DoesQualifyForReporting(ProductPriceOffersReport otherReport, double priceDifferencePercentage)
    {
        var thisBestOffer = ProductPriceOffers.MinBy(offer => offer.Price);
        var otherBestOffer = otherReport.ProductPriceOffers.MinBy(offer => offer.Price);
        if (Math.Abs(thisBestOffer.Price - otherBestOffer.Price) > thisBestOffer.Price * (decimal)priceDifferencePercentage / 100)
            return true;
        return false;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}

public readonly record struct SingularProductPriceOffer(decimal Price, string ShopAddressName);