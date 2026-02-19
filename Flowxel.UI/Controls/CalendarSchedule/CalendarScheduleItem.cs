using Avalonia.Media;

namespace Flowxel.UI.Controls.CalendarSchedule;

public class CalendarScheduleItem
{
    public string? Title { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public IBrush? Color { get; set; }
    public string? Description { get; set; }
}

public enum CalendarViewMode
{
    Week,
    Month
}
