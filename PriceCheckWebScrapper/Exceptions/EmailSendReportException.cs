using System.Runtime.Serialization;

namespace PriceCheckWebScrapper.Exceptions;

[Serializable]
public class EmailSendReportException : Exception
{
    public EmailSendReportException()
    {
    }

    protected EmailSendReportException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public EmailSendReportException(string? message) : base(message)
    {
    }

    public EmailSendReportException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}