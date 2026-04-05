namespace GlobalDataCreatorETL.UI.Helpers;

public static class DateHelpers
{
    public static readonly IReadOnlyList<string> MonthNames =
        new[] { "January","February","March","April","May","June",
                "July","August","September","October","November","December" };

    public static IReadOnlyList<int> YearRange()
    {
        int current = DateTime.Now.Year;
        return Enumerable.Range(current - 10, 15).ToList();
    }
}
