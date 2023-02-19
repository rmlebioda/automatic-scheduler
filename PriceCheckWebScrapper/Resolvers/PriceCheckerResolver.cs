using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using PriceCheckWebScrapper.Checkers.Ceneo;

namespace PriceCheckWebScrapper.Resolvers;

internal static class PriceCheckerResolver
{
    public static IPriceChecker Resolve(Uri uri, PriceCheckerOptions priceCheckerOptions, ILogger logger)
    {
        return uri.Host switch
        {
            "www.ceneo.pl" => new CeneoPriceChecker(priceCheckerOptions, logger),
            _ => throw new ArgumentException(string.Format("Unsupported webpage: {0}", uri))
        };
    }
}