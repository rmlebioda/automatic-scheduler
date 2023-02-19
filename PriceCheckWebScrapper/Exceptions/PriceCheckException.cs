using System.Runtime.Serialization;
using OpenQA.Selenium;

namespace PriceCheckWebScrapper.Exceptions;

[Serializable]
public class PriceCheckException : Exception
{
    public PriceCheckExceptionType ExceptionReason { get; }

    /// <summary>
    /// Whenever raised exception during price check is fatal,
    /// that is even after repeating process, the result will not change,
    /// like for example in case of providing invalid log in credentials
    /// </summary>
    public bool IsExceptionReasonFatal
    {
        get
        {
            return ExceptionReason switch
            {
                PriceCheckExceptionType.Unknown or PriceCheckExceptionType.FailedToFindElementByXPath => false,
                _ => true
            };
        }
    }

    public PriceCheckException()
    {
    }

    protected PriceCheckException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public PriceCheckException(string? message) : base(message)
    {
    }

    public PriceCheckException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public PriceCheckException(PriceCheckExceptionType exceptionReason, string message) : this(message)
    {
        ExceptionReason = exceptionReason;
    }

    public static PriceCheckException FromBy(By by)
    {
        return new PriceCheckException(PriceCheckExceptionType.FailedToFindElementByXPath,
            string.Format("Failed to find element {0}.", by));
    }
}