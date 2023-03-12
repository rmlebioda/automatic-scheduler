namespace CinemaCity.Models;

public class CinemaCityEmailReport
{
    public CinemaCityCinemaReport Report { get; init; }
    public IEnumerable<CinemaCityShowtime> Showtimes { get; init; }
}