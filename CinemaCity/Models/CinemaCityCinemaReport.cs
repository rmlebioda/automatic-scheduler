namespace CinemaCity.Models;

public readonly record struct CinemaCityCinemaReport(string Url, string CinemaName, string CinemaAddress,
    IEnumerable<CinemaCityShowtime> Showtimes);