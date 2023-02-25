using System.Globalization;
using OpenQA.Selenium;
using PriceCheckWebScrapper.Core;

#nullable enable

namespace PriceCheckWebScrapper.Checkers.Ceneo.Models;

public record ProductOffer(IWebElement WebElement)
{
    private const string ProductPriceClassName = "product-offer__product__price";
    private readonly By ByProductPriceClassName = By.ClassName(ProductPriceClassName);
    private const string PriceSpanClassName = "price";
    private readonly By ByPriceSpanClassName = By.ClassName(PriceSpanClassName);
    private const string PriceValueClassName = "value";
    private readonly By ByPriceValueClassName = By.ClassName(PriceValueClassName);
    private const string PricePennyClassName = "penny";
    private readonly By ByPricePennyClassName = By.ClassName(PricePennyClassName);

    public decimal Price
    {
        get
        {
            var priceElement = WebElement.FindElement(ByProductPriceClassName);
            var priceSpan = priceElement.FindElement(ByPriceSpanClassName);
            var value = priceSpan.FindElement(ByPriceValueClassName);
            var penny = priceSpan.FindElement(ByPricePennyClassName);
            return decimal.Parse(value.Text + penny.Text, new CultureInfo("pl-PL"));
        }
    }

    private const string GoToShopClassName = "store-logo";
    private readonly By ByGoToShopClassName = By.ClassName(GoToShopClassName);

    private IWebElement GoToShopImgElement
    {
        get
        {
            var goToShopElement = WebElement.FindElement(ByGoToShopClassName);
            return goToShopElement.FindElement(By.TagName("img"));
        }
    }
    
    public string GetVendorAddress
    {
        get
        {
            return GoToShopImgElement.GetAttribute("alt");
        }
    }

    public SingularProductPriceOffer GetSingularProductPriceOffer()
    {
        return new SingularProductPriceOffer(Price, GetVendorAddress);
    }
}
