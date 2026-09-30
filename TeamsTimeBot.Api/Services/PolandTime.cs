namespace TeamsTimeBot.Api.Services;

public static class PolandTime
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    private static TimeZoneInfo ResolveZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "Central European Standard Time");
        }
    }

    public static DateTime Now =>
        TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            Zone);

    public static DateTime ToUtc(DateTime localDateTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(
                localDateTime,
                DateTimeKind.Unspecified),
            Zone);
    }
}
