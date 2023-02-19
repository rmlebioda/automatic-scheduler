namespace PriceCheckWebScrapper.Exceptions;

public enum PriceCheckExceptionType
{
    Unknown = 0,
    FailedToFindElementByXPath,
    MissingCredentials,
    InvalidCredentials,
    OtherLogInFailure
}