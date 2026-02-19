using System;
using System.Collections.Generic;
using global::Avalonia.Media;
using Flowxel.UI.Controls.CalendarSchedule;

namespace Flowxel.UITester.ViewModels;

public class SchedulePageViewModel : ViewModelBase
{
    public SchedulePageViewModel()
    {
        var today = DateTimeOffset.Now.Date;
        var monday = today.AddDays(-((int)today.DayOfWeek - 1 + 7) % 7);

        Items = new List<CalendarScheduleItem>
        {
            new()
            {
                Title = "Team Standup",
                Start = monday.AddHours(9),
                End = monday.AddHours(9.5),
                Color = new SolidColorBrush(Color.Parse("#3574F0")),
                Description = "Daily standup meeting"
            },
            new()
            {
                Title = "Code Review",
                Start = monday.AddHours(14),
                End = monday.AddHours(15),
                Color = new SolidColorBrush(Color.Parse("#59A869")),
                Description = "Review pull requests"
            },
            new()
            {
                Title = "Sprint Planning",
                Start = monday.AddDays(1).AddHours(10),
                End = monday.AddDays(1).AddHours(12),
                Color = new SolidColorBrush(Color.Parse("#E8A33D")),
                Description = "Plan next sprint"
            },
            new()
            {
                Title = "Lunch with Team",
                Start = monday.AddDays(2).AddHours(12),
                End = monday.AddDays(2).AddHours(13),
                Color = new SolidColorBrush(Color.Parse("#9C6BC1")),
                Description = "Team lunch"
            },
            new()
            {
                Title = "Architecture Review",
                Start = monday.AddDays(2).AddHours(15),
                End = monday.AddDays(2).AddHours(16.5),
                Color = new SolidColorBrush(Color.Parse("#F75464")),
                Description = "Review system architecture"
            },
            new()
            {
                Title = "1:1 with Manager",
                Start = monday.AddDays(3).AddHours(11),
                End = monday.AddDays(3).AddHours(11.5),
                Color = new SolidColorBrush(Color.Parse("#3574F0")),
                Description = "Weekly 1:1"
            },
            new()
            {
                Title = "Release Prep",
                Start = monday.AddDays(4).AddHours(9),
                End = monday.AddDays(4).AddHours(10),
                Color = new SolidColorBrush(Color.Parse("#E8A33D")),
                Description = "Prepare release notes"
            },
            new()
            {
                Title = "Team Retrospective",
                Start = monday.AddDays(4).AddHours(16),
                End = monday.AddDays(4).AddHours(17),
                Color = new SolidColorBrush(Color.Parse("#59A869")),
                Description = "Sprint retrospective"
            },
            new()
            {
                Title = "Dentist Appointment",
                Start = today.AddHours(8),
                End = today.AddHours(9),
                Color = new SolidColorBrush(Color.Parse("#9C6BC1")),
                Description = "Regular checkup"
            },
            new()
            {
                Title = "Project Demo",
                Start = today.AddHours(14),
                End = today.AddHours(15.5),
                Color = new SolidColorBrush(Color.Parse("#3574F0")),
                Description = "Demo new features to stakeholders"
            },
        };
    }
    
    public IReadOnlyList<CalendarScheduleItem> Items { get; }
}
