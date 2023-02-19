using System.Text;

namespace PriceCheckWebScrapper.Core.Mail;

public class MailManager
{
    private List<ProductPriceOffersReport> _productPriceOffersReports;
    private EmailProviderSendingOptions _options;
    
    private static string NL => Environment.NewLine;
    
    public MailManager(EmailProviderSendingOptions options)
    {
        _options = options;
        _productPriceOffersReports = new();
    }

    public string TargetEmail => _options.TargetEmail;
    public string Title => $"Price update at {DateTime.UtcNow}";

    public string GetBody()
    {
        var builder = new StringBuilder();
        foreach (var report in _productPriceOffersReports)
        {
            builder.Append(GetReportBody(report)).Append(NL);
        }

        return builder.ToString();
    }

    private string GetReportBody(ProductPriceOffersReport report)
    {
        var builder = new StringBuilder();
        builder.Append($"Item: {report.ProductName}").Append(NL);
        builder.Append($"Url: {report.Uri.AbsoluteUri}").Append(NL);
        var bestOffer = report.ProductPriceOffers.MinBy(offer => offer.Price);
        builder.Append($"New price: {bestOffer.Price} at {bestOffer.ShopAddressName}").Append(NL);
        return builder.ToString();
    }
}