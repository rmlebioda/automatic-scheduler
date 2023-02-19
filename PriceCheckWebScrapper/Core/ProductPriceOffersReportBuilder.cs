namespace PriceCheckWebScrapper.Core;

public class ProductPriceOffersReportBuilder
{
    private readonly Uri _uri;
    private string _name;
    private readonly List<SingularProductPriceOffer> _offers = new();
    
    public ProductPriceOffersReportBuilder(Uri uri)
    {
        _uri = uri;
        _name = uri.AbsoluteUri;
    }

    public void SetName(string name)
    {
        _name = name;
    }

    public void Add(SingularProductPriceOffer offer)
    {
        _offers.Add(offer);
    }

    public void AddRange(IEnumerable<SingularProductPriceOffer> offers)
    {
        _offers.AddRange(offers);
    }

    public void AddReport(ProductPriceOffersReport report)
    {
        SetName(report.ProductName);
        AddRange(report.ProductPriceOffers);
    }

    public ProductPriceOffersReport Build()
    {
        return new ProductPriceOffersReport(_uri, _name, _offers);
    }
}