using CommandLine;
using CommandLine.Text;
using PriceCheckWebScrapper;
using Serilog.Events;

namespace AutomaticScheduler.Console;

public class Options
{
    [Option('e', "email", Required = true, HelpText = "Email of sender")]
    public string EmailManagerSenderEmail { get; set; }
    
    [Option('p', "password", Required = true, HelpText = "Password for email of sender")]
    public string EmailManagerSenderPassword { get; set; }
    
    [Option('t', "target", Required = true, HelpText = "Target email, which will receive notifications")]
    public string EmailManagerTargetEmail { get; set; }
    
    [Option('u', "urls", Required = true, HelpText = "List of URL's to check")]
    public IEnumerable<string> UrlsToCheck { get; set; }
    
    [Option('i', "interval", Required = true, HelpText = "Interval in minutes, how often program should execute checks")]
    public double Interval { get; set; }
    
    [Option('d', "start-date-time", Required = true, HelpText = "Valid date time (for example in ISO 8601 standard), on which task should start. If passed with past date, program will execute checks immediately.")]
    public string StartDateTime { get; set; }
    
    [Option('f', "log-file", Required = false, HelpText = "Path to file, where logs should be saved")]
    public string? LogFile { get; set; }
    
    [Option('c', "log-console", Required = false, HelpText = "Whenever logs should be written to console")]
    public bool? WriteToConsole { get; set; }
    
    [Option('v', "verbosity", Required = false, HelpText = "Verbosity of logs")]
    public LogEventLevel? Verbosity { get; set; }
    
    [Option('w', "webdriver", Required = false, HelpText = "Sets desired webdriver (supported: Chrome, Firefox), defaults to Chrome")]
    public WebDriverType? WebDriver { get; set; }


    [Usage(ApplicationAlias = "AutomaticScheduler.Console")]
    public static IEnumerable<Example> Examples
    {
        get
        {
            yield return new Example("Call example",
                new Options
                {
                    EmailManagerSenderEmail = "my.sender.email@outlook.com",
                    EmailManagerSenderPassword = "my.password",
                    EmailManagerTargetEmail = "target.email.receiver@gmail.com",
                    Interval = 60.0,
                    Verbosity = LogEventLevel.Debug,
                    LogFile = "/logs/autoscheduler.txt",
                    StartDateTime = DateTime.MinValue.ToString(),
                    UrlsToCheck = new List<string> { "https://fake.website.com/item/12341234" },
                    WriteToConsole = true
                });
        }
    }
}