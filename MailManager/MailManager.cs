using System.Net;
using System.Net.Mail;

namespace MailManager;

public record MailManager
{
    private readonly MailAddress _senderEmailAddress;
    private readonly EmailProvider _emailProvider;
    private readonly string _senderEmailPassword;

    public MailManager(EmailProvider emailProvider, string senderEmailAddress,
        string senderEmailPassword)
    {
        if (emailProvider == EmailProvider.Unknown)
            throw new ArgumentException("Invalid Email provider");

        _emailProvider = emailProvider;
        _senderEmailAddress = new MailAddress(senderEmailAddress, senderEmailAddress);
        _senderEmailPassword = senderEmailPassword;
    }

    private SmtpClient CreateClient()
    {
        switch (_emailProvider)
        {
            case EmailProvider.Gmail:
            {
                return new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_senderEmailAddress.Address, _senderEmailPassword)
                };
            }
            case EmailProvider.Outlook:
            {
                return new SmtpClient
                {
                    Host = "smtp-mail.outlook.com",
                    Port = 587,
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_senderEmailAddress.Address, _senderEmailPassword)
                };
            }
            default:
            {
                throw new ArgumentException(string.Format("Email provider {0} is not supported",
                    _emailProvider.ToString()));
            }
        }
        
    }

    public async Task SendEmailAsync(string targetEmail, string subject, string body)
    {
        await SendEmailAsync(targetEmail, subject, body, new string[] { });
    }

    public async Task SendEmailAsync(string targetEmail, string subject, string body, IEnumerable<string> attachments)
    {
        using var mailMessage = new MailMessage(_senderEmailAddress.Address, targetEmail);
        mailMessage.Subject = subject;
        mailMessage.Body = body;
        foreach (var attachment in attachments) mailMessage.Attachments.Add(new Attachment(attachment));
        using var smtpClient = CreateClient();
        await smtpClient.SendMailAsync(mailMessage);
    }

    public void SendEmail(string targetEmail, string subject, string body)
    {
        SendEmail(targetEmail, subject, body, new string[] { });
    }
    
    public void SendEmail(string targetEmail, string subject, string body, IEnumerable<string> attachments)
    {
        using var mailMessage = new MailMessage(_senderEmailAddress.Address, targetEmail);
        mailMessage.Subject = subject;
        mailMessage.Body = body;
        foreach (var attachment in attachments) mailMessage.Attachments.Add(new Attachment(attachment));
        using var smtpClient = CreateClient();
        smtpClient.Send(mailMessage);
    }
}