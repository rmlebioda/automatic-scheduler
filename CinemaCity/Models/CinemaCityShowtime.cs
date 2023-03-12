namespace CinemaCity.Models;

public readonly record struct CinemaCityShowtime(DateTime DateTime, bool CanReserve, string? ErrorCode = null,
    string? ReservationError = null,
    string? ReservationDetailsError = null);