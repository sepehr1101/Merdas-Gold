using System.Globalization;

namespace MerdasGold.Features.OperationLogs.Models;

public static class OperationLogDate
{
    private static readonly TimeZoneInfo Tehran = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");

    public static DateTime Today => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Tehran).Date;

    public static string NormalizeDigits(string? value) => string.Concat((value ?? string.Empty).Select(character =>
        character is >= '۰' and <= '۹' ? (char)('0' + character - '۰') :
        character is >= '٠' and <= '٩' ? (char)('0' + character - '٠') : character));

    public static bool TryParse(string? value, out DateTime? date)
    {
        date = null;
        if (string.IsNullOrWhiteSpace(value)) return true;

        var parts = NormalizeDigits(value).Trim().Split('/');
        if (parts.Length != 3 || parts[0].Length != 4 || parts[1].Length is < 1 or > 2 || parts[2].Length is < 1 or > 2
            || parts.Any(part => part.Any(character => character is < '0' or > '9'))
            || !int.TryParse(parts[0], out var year) || !int.TryParse(parts[1], out var month) || !int.TryParse(parts[2], out var day))
            return false;

        try
        {
            date = new PersianCalendar().ToDateTime(year, month, day, 0, 0, 0, 0);
            // Leave room for the exclusive end-of-day boundary.
            return date.Value.Date < DateTime.MaxValue.Date;
        }
        catch (ArgumentOutOfRangeException) { return false; }
    }

    public static string FormatDate(DateTime date)
    {
        var calendar = new PersianCalendar();
        return FormattableString.Invariant($"{calendar.GetYear(date):0000}/{calendar.GetMonth(date):00}/{calendar.GetDayOfMonth(date):00}");
    }

    public static DateTime FromUtc(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Tehran);

    public static DateTime? ToUtcBoundary(DateTime? date, bool endExclusive = false)
    {
        if (date is null) return null;
        var local = DateTime.SpecifyKind(date.Value.Date.AddDays(endExclusive ? 1 : 0), DateTimeKind.Unspecified);
        // Historical daylight-saving transitions in Iran can skip local midnight.
        while (Tehran.IsInvalidTime(local)) local = local.AddMinutes(1);
        if (Tehran.IsAmbiguousTime(local))
            return new DateTimeOffset(local, Tehran.GetAmbiguousTimeOffsets(local).Max()).UtcDateTime;
        return TimeZoneInfo.ConvertTimeToUtc(local, Tehran);
    }
}
