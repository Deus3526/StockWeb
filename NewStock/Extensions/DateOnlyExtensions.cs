namespace NewStock.Extensions;

/// <summary>
/// Calendar week (Monday-first) / month. FinMind week-K date is calendar Monday even if non-trading.
/// </summary>
public static class DateOnlyExtensions
{
    /// <summary>Monday of the week containing the date (week starts Monday).</summary>
    public static DateOnly GetMondayOfCalendarWeek(this DateOnly date)
    {
        var daysFromMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-daysFromMonday);
    }

    public static bool AreInDifferentCalendarWeeks(this DateOnly a, DateOnly b) =>
        a.GetMondayOfCalendarWeek() != b.GetMondayOfCalendarWeek();

    public static DateOnly GetFirstDayOfCalendarMonth(this DateOnly date) =>
        new(date.Year, date.Month, 1);

    public static bool AreInDifferentCalendarMonths(this DateOnly a, DateOnly b) =>
        a.GetFirstDayOfCalendarMonth() != b.GetFirstDayOfCalendarMonth();
}