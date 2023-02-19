using OpenQA.Selenium;

namespace PriceCheckWebScrapper.Checkers.Ceneo.Models;

public record ProductOffers(IWebElement WebElement)
{
    private const string ProductOfferListXPath = "product-offers__list";
    private const string ProductOffersXPath = "product-offers__list__item";
    
    public IEnumerable<ProductOffer> GetProductOffers()
    {
        var offerList = WebElement.FindElements(By.ClassName(ProductOfferListXPath));
        return offerList.Select(offerListElement => offerListElement.FindElements(By.ClassName(ProductOffersXPath)))
            .SelectMany(x => x)
            .Select(element => new ProductOffer(element));
    }
}