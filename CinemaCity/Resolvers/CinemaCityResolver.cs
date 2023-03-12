namespace CinemaCity.Resolvers;

internal static class CinemaCityResolver
{
    public static bool IsSupported(Uri uri)
    {
        return uri.Host switch
        {
            "www.cinema-city.pl" => true,
            _ => false
        };
    }
}