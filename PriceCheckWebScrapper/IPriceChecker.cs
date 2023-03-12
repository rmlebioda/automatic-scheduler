using OpenQA.Selenium;
using PriceCheckWebScrapper.Core;

namespace PriceCheckWebScrapper;

internal interface IPriceChecker
{
    ProductPriceOffersReport CheckPrice(IWebDriver webDriver, Uri uri);
}
