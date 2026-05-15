namespace NewStock.Extensions;

/// <summary>
/// 曆週／曆月 相關擴充；週一、月初與 FinMind 週／月 K 參數口徑一致。
/// </summary>
public static class DateOnlyExtensions
{
    /// <summary>
    /// 含 <paramref name="date"/> 在內之該曆週的週一。
    /// </summary>
    public static DateOnly GetMondayOfCalendarWeek(this DateOnly date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-daysSinceMonday);
    }

    /// <summary>
    /// 兩個日期是否分屬不同曆週。
    /// </summary>
    public static bool AreInDifferentCalendarWeeks(this DateOnly a, DateOnly b) =>
        a.GetMondayOfCalendarWeek() != b.GetMondayOfCalendarWeek();

    /// <summary>
    /// 含 <paramref name="date"/> 在內之該曆月一日（與 FinMind 月 K 之月初參數一致）。
    /// </summary>
    public static DateOnly GetFirstDayOfCalendarMonth(this DateOnly date) =>
        new DateOnly(date.Year, date.Month, 1);

    /// <summary>
    /// 兩個日期是否分屬不同曆月（僅比對年、月）。
    /// </summary>
    public static bool AreInDifferentCalendarMonths(this DateOnly a, DateOnly b) =>
        a.GetFirstDayOfCalendarMonth() != b.GetFirstDayOfCalendarMonth();
}
