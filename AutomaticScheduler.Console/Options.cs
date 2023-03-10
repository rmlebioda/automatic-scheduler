using System.Reflection;
using System.Text;
using CommandLine;
using CommandLine.Text;
using Serilog.Events;
using WebDriverFramework;

namespace AutomaticScheduler.Console;

public class Options
{
    private static readonly string[] SecretProperties = { nameof(CeneoPassword), nameof(EmailManagerSenderPassword) };

    [Option('e', "email", Required = true, HelpText = "Email of sender")]
    public string EmailManagerSenderEmail { get; set; }

    [Option('p', "email-password", Required = true, HelpText = "Password for email of sender")]
    public string EmailManagerSenderPassword { get; set; }

    [Option('t', "email-target", Required = true, HelpText = "Target email, which will receive notifications")]
    public string EmailManagerTargetEmail { get; set; }

    [Option('u', "urls", Required = true, HelpText = "List of URL's to check")]
    public IEnumerable<string> UrlsToCheck { get; set; }

    [Option('i', "interval", Required = true,
        HelpText = "Interval in minutes, how often program should execute checks")]
    public double Interval { get; set; }

    [Option('D', "price-difference", Default = 1, Required = false,
        HelpText =
            "Price difference in percentage, to which change of best price is considered as worthy of sending email")]
    public double PriceDifferencePercentage { get; set; }

    [Option('d', "start-date-time", Required = false,
        HelpText =
            "Valid date time (for example in ISO 8601 standard), on which task should start. If passed with past date, program will execute checks immediately.")]
    public string? StartDateTime { get; set; }

    [Option('f', "log-file", Required = false, HelpText = "Path to basic file, to which logs should be saved")]
    public string? LogFile { get; set; }

    [Option('o', "output-template",
        Default =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}({Properties:j}){NewLine}{Exception}",
        Required = false,
        HelpText =
            "Log template, for more information check https://github.com/serilog/serilog/wiki/Configuration-Basics#output-templates")]
    public string LogTemplate { get; set; } = default!;

    [Option('c', "log-console", Required = false, Default = false,
        HelpText = "Whenever logs should be written to console")]
    public bool WriteToConsole { get; set; }

    [Option('v', "verbosity", Required = false, HelpText = "Verbosity of logs")]
    public LogEventLevel? Verbosity { get; set; }

    [Option('w', "webdriver", Required = false,
        HelpText = "Sets desired webdriver (supported: Chrome, Firefox), defaults to Chrome")]
    public WebDriverType? WebDriver { get; set; }

    [Option('r', "retries", Default = 2, Required = false,
        HelpText = "How many times check should be retried in case of failure")]
    public int Retries { get; set; }

    [Option("timeout", Default = 30.0, Required = false,
        HelpText = "Timeout in seconds for loading webpage and all operations like mouse clicks, logging in etc.")]
    public double Timeout { get; set; }

    [Option("ceneo-login", Required = false, HelpText = "Login for ceneo.pl")]
    public string? CeneoLogin { get; set; }

    [Option("ceneo-password", Required = false, HelpText = "Password for ceneo.pl")]
    public string? CeneoPassword { get; set; }

    [Option('s', "make-screenshots", Required = false,
        HelpText =
            "Whenever screenshots should be made after failures exceed maximum amount of retries or failure (defaults to false)")]
    public bool? MakeScreenshotAfterFailure { get; set; }
    
    [Option("cc-cinema-name", Required = false, HelpText = "Cinema name to search for on cinema-city")]
    public string? CinemaCityName { get; set; }
    
    [Option("cc-cinema-town-name", Required = false, HelpText = "Town, in which cinema is located on cinema-city")]
    public string? CinemaCityTownName { get; set; }
    
    [Option("cc-cinema-vip", Required = false, HelpText = "If VIP is required")]
    public bool? CinemaCityVip { get; set; }

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

    public override string ToString()
    {
        var builder = new StringBuilder();

        foreach (var property in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            builder.Append(property.Name).Append("=");
            if (!SecretProperties.Contains(property.Name))
                builder.Append(property.GetValue(this));
            else
                builder.Append("******");
            builder.Append(',');
        }

        return builder.ToString().TrimEnd(',');
    }
}