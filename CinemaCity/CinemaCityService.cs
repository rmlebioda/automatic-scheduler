using System.Text;
using CinemaCity.Checkers;
using CinemaCity.Models;
using CinemaCity.Resolvers;
using Microsoft.Extensions.Logging;
using WebDriverFramework;

namespace CinemaCity;

public class CinemaCityService
{
    private readonly CinemaCityOptions _cinemaCityOptions;
    private readonly ILogger _logger;
    private readonly Dictionary<string, CinemaCityCinemaReport> _reports = new();
    private readonly List<Uri> _uris;

    public CinemaCityService(CinemaCityOptions cinemaCityOptions, ILogger logger, List<Uri> uris)
    {
        _cinemaCityOptions = cinemaCityOptions;
        _logger = logger;
        _uris = uris;
    }

    public static bool IsSupported(Uri uri)
    {
        return CinemaCityResolver.IsSupported(uri);
    }

    public async Task CheckAsync()
    {
        var webDriver = WebDriverManager.CreateNewWebDriver(_cinemaCityOptions.WebDriverOptions);
        try
        {
            var checker = new CinemaCityChecker(_cinemaCityOptions, webDriver, _logger);

            var showtimeReports = new List<CinemaCityCinemaReport>();

            foreach (var uri in _uris)
            {
                showtimeReports.Add(CheckUri(checker, uri));
            }

            Report(showtimeReports);
        }
        finally
        {
            webDriver.Close();
        }
    }

    private CinemaCityCinemaReport CheckUri(CinemaCityChecker checker, Uri uri)
    {
        return checker.Check(uri);
    }

    private void Report(List<CinemaCityCinemaReport> showtimeReports)
    {
        var showtimesToReport = new List<CinemaCityEmailReport>();

        foreach (var report in showtimeReports)
        {
            if (_reports.TryGetValue(report.Url, out var lastReport))
            {
                showtimesToReport.Add(new CinemaCityEmailReport
                {
                    Report = report,
                    Showtimes = GetChangesInShowtimes(lastReport, report)
                });
            }
            else
            {
                showtimesToReport.Add(new CinemaCityEmailReport
                {
                    Report = report,
                    Showtimes = report.Showtimes.ToList()
                });
            }

            if (_reports.ContainsKey(report.Url))
            {
                _reports[report.Url] = report;
            }
            else
            {
                _reports.Add(report.Url, report);
            }
        }

        if (showtimesToReport.Select(report => report
                .Showtimes).SelectMany(x => x).Any())
        {
            SendEmail(showtimesToReport);
        }
    }

    private IList<CinemaCityShowtime> GetChangesInShowtimes(CinemaCityCinemaReport oldReport,
        CinemaCityCinemaReport newReport)
    {
        var changes = new List<CinemaCityShowtime>();
        foreach (var showtime in newReport.Showtimes)
        {
            var oldShowtime = oldReport.Showtimes.Cast<CinemaCityShowtime?>()
                .FirstOrDefault(s => s!.Value.DateTime == showtime.DateTime);
            if (oldShowtime is null)
            {
                changes.Add(showtime);
            }
            else
            {
                if (oldShowtime!.Value.CanReserve != showtime.CanReserve ||
                    oldShowtime!.Value.ErrorCode != showtime.ErrorCode)
                {
                    changes.Add(showtime);
                }
            }
        }

        return changes;
    }

    private void SendEmail(List<CinemaCityEmailReport> showtimesToReport)
    {
        var emailBuilder = new StringBuilder();
        foreach (var report in showtimesToReport)
        {
            emailBuilder.Append("Cinema ").Append(report.Report.CinemaName).Append(Environment.NewLine);
            emailBuilder.Append("Dates: ").Append(Environment.NewLine);
            foreach (var showtime in report.Showtimes)
            {
                emailBuilder.Append("- ")
                    .Append(showtime.DateTime.ToString("yyyy/MM/dd HH:mm:ss"))
                    .Append(" - is available? ")
                    .Append(showtime.CanReserve);

                if (!showtime.CanReserve)
                {
                    emailBuilder
                        .Append(" (")
                        .Append(showtime.ErrorCode)
                        .Append("): ")
                        .Append(showtime.ReservationError)
                        .Append(" ")
                        .Append(showtime.ReservationDetailsError);
                }

                emailBuilder.Append(Environment.NewLine);
            }
        }

        if (emailBuilder.Length > 0)
        {
            _cinemaCityOptions.MailManager.SendEmail(
                _cinemaCityOptions.EmailProviderSendingOptions.TargetEmail,
                "Cinema city changes",
                emailBuilder.ToString());
        }
    }
}