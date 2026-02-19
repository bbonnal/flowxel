using Avalonia.Controls.Primitives;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using System.Globalization;

namespace Flowxel.UI.Controls.CalendarSchedule;

public class CalendarScheduleControl : TemplatedControl
{
    private const double HourHeight = 60.0;

    public static readonly StyledProperty<DateTimeOffset> DisplayDateProperty =
        AvaloniaProperty.Register<CalendarScheduleControl, DateTimeOffset>(
            nameof(DisplayDate),
            DateTimeOffset.Now,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<CalendarViewMode> ViewModeProperty =
        AvaloniaProperty.Register<CalendarScheduleControl, CalendarViewMode>(
            nameof(ViewMode),
            CalendarViewMode.Month,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable<CalendarScheduleItem>?> ItemsProperty =
        AvaloniaProperty.Register<CalendarScheduleControl, IEnumerable<CalendarScheduleItem>?>(nameof(Items));

    public static readonly StyledProperty<DateTimeOffset?> SelectedDateProperty =
        AvaloniaProperty.Register<CalendarScheduleControl, DateTimeOffset?>(
            nameof(SelectedDate),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<CalendarScheduleItem?> SelectedItemProperty =
        AvaloniaProperty.Register<CalendarScheduleControl, CalendarScheduleItem?>(
            nameof(SelectedItem),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DayOfWeek> FirstDayOfWeekProperty =
        AvaloniaProperty.Register<CalendarScheduleControl, DayOfWeek>(
            nameof(FirstDayOfWeek),
            DayOfWeek.Monday);

    public DateTimeOffset DisplayDate
    {
        get => GetValue(DisplayDateProperty);
        set => SetValue(DisplayDateProperty, value);
    }

    public CalendarViewMode ViewMode
    {
        get => GetValue(ViewModeProperty);
        set => SetValue(ViewModeProperty, value);
    }

    public IEnumerable<CalendarScheduleItem>? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public DateTimeOffset? SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public CalendarScheduleItem? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public DayOfWeek FirstDayOfWeek
    {
        get => GetValue(FirstDayOfWeekProperty);
        set => SetValue(FirstDayOfWeekProperty, value);
    }

    // Template parts - Header
    private Button? _previousButton;
    private Button? _nextButton;
    private Button? _todayButton;
    private TextBlock? _headerTitle;
    private Button? _weekButton;
    private Button? _monthButton;

    // Template parts - Mini Calendar
    private Button? _miniCalPrevButton;
    private Button? _miniCalNextButton;
    private TextBlock? _miniCalTitle;
    private Grid? _miniCalGrid;

    // Template parts - Month View
    private Grid? _monthViewDayHeaders;
    private Grid? _monthViewGrid;

    // Template parts - Week View
    private Grid? _weekViewDayHeaders;
    private ScrollViewer? _weekViewScrollViewer;
    private Grid? _weekViewTimeGrid;

    // Template parts - View containers
    private Control? _monthView;
    private Control? _weekView;

    private DateTimeOffset _miniCalDisplayMonth;
    private bool _initialScrollDone;

    private readonly List<(Border border, CalendarScheduleItem item, IBrush baseBrush)> _appointmentBorders = new();

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        UnwireTemplatePartEvents();

        // Header
        _previousButton = e.NameScope.Find<Button>("PART_PreviousButton");
        _nextButton = e.NameScope.Find<Button>("PART_NextButton");
        _todayButton = e.NameScope.Find<Button>("PART_TodayButton");
        _headerTitle = e.NameScope.Find<TextBlock>("PART_HeaderTitle");
        _weekButton = e.NameScope.Find<Button>("PART_WeekButton");
        _monthButton = e.NameScope.Find<Button>("PART_MonthButton");

        // Mini Calendar
        _miniCalPrevButton = e.NameScope.Find<Button>("PART_MiniCalPrevButton");
        _miniCalNextButton = e.NameScope.Find<Button>("PART_MiniCalNextButton");
        _miniCalTitle = e.NameScope.Find<TextBlock>("PART_MiniCalTitle");
        _miniCalGrid = e.NameScope.Find<Grid>("PART_MiniCalGrid");

        // Month View
        _monthViewDayHeaders = e.NameScope.Find<Grid>("PART_MonthViewDayHeaders");
        _monthViewGrid = e.NameScope.Find<Grid>("PART_MonthViewGrid");

        // Week View
        _weekViewDayHeaders = e.NameScope.Find<Grid>("PART_WeekViewDayHeaders");
        _weekViewScrollViewer = e.NameScope.Find<ScrollViewer>("PART_WeekViewScrollViewer");
        _weekViewTimeGrid = e.NameScope.Find<Grid>("PART_WeekViewTimeGrid");

        // View containers
        _monthView = e.NameScope.Find<Control>("PART_MonthView");
        _weekView = e.NameScope.Find<Control>("PART_WeekView");

        WireTemplatePartEvents();

        _miniCalDisplayMonth = new DateTimeOffset(DisplayDate.Year, DisplayDate.Month, 1, 0, 0, 0, DisplayDate.Offset);
        _initialScrollDone = false;

        UpdatePseudoClasses();
        Rebuild();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        UnwireTemplatePartEvents();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DisplayDateProperty ||
            change.Property == ItemsProperty ||
            change.Property == FirstDayOfWeekProperty)
        {
            if (change.Property == DisplayDateProperty)
            {
                _miniCalDisplayMonth = new DateTimeOffset(DisplayDate.Year, DisplayDate.Month, 1, 0, 0, 0, DisplayDate.Offset);
                _initialScrollDone = false;
            }
            Rebuild();
        }
        else if (change.Property == ViewModeProperty)
        {
            UpdatePseudoClasses();
            _initialScrollDone = false;
            Rebuild();
        }
        else if (change.Property == SelectedItemProperty)
        {
            UpdateAppointmentSelection();
        }
        else if (change.Property == SelectedDateProperty)
        {
            Rebuild();
        }
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":week", ViewMode == CalendarViewMode.Week);
        PseudoClasses.Set(":month", ViewMode == CalendarViewMode.Month);
    }

    private void NavigatePrevious()
    {
        DisplayDate = ViewMode == CalendarViewMode.Month
            ? DisplayDate.AddMonths(-1)
            : DisplayDate.AddDays(-7);
    }

    private void NavigateNext()
    {
        DisplayDate = ViewMode == CalendarViewMode.Month
            ? DisplayDate.AddMonths(1)
            : DisplayDate.AddDays(7);
    }

    private void NavigateToday()
    {
        DisplayDate = DateTimeOffset.Now;
        SelectedDate = DateTimeOffset.Now;
    }

    private void MiniCalNavigate(int monthDelta)
    {
        _miniCalDisplayMonth = _miniCalDisplayMonth.AddMonths(monthDelta);
        UpdateMiniCalendar();
    }

    private void WireTemplatePartEvents()
    {
        if (_previousButton != null)
            _previousButton.Click += OnPreviousButtonClick;
        if (_nextButton != null)
            _nextButton.Click += OnNextButtonClick;
        if (_todayButton != null)
            _todayButton.Click += OnTodayButtonClick;
        if (_weekButton != null)
            _weekButton.Click += OnWeekButtonClick;
        if (_monthButton != null)
            _monthButton.Click += OnMonthButtonClick;
        if (_miniCalPrevButton != null)
            _miniCalPrevButton.Click += OnMiniCalPreviousButtonClick;
        if (_miniCalNextButton != null)
            _miniCalNextButton.Click += OnMiniCalNextButtonClick;

        if (_weekViewTimeGrid != null)
        {
            _weekViewTimeGrid.Background = Brushes.Transparent;
            _weekViewTimeGrid.PointerPressed += OnWeekViewTimeGridPointerPressed;
        }
    }

    private void UnwireTemplatePartEvents()
    {
        if (_previousButton != null)
            _previousButton.Click -= OnPreviousButtonClick;
        if (_nextButton != null)
            _nextButton.Click -= OnNextButtonClick;
        if (_todayButton != null)
            _todayButton.Click -= OnTodayButtonClick;
        if (_weekButton != null)
            _weekButton.Click -= OnWeekButtonClick;
        if (_monthButton != null)
            _monthButton.Click -= OnMonthButtonClick;
        if (_miniCalPrevButton != null)
            _miniCalPrevButton.Click -= OnMiniCalPreviousButtonClick;
        if (_miniCalNextButton != null)
            _miniCalNextButton.Click -= OnMiniCalNextButtonClick;
        if (_weekViewTimeGrid != null)
            _weekViewTimeGrid.PointerPressed -= OnWeekViewTimeGridPointerPressed;
    }

    private void OnPreviousButtonClick(object? sender, RoutedEventArgs e) => NavigatePrevious();
    private void OnNextButtonClick(object? sender, RoutedEventArgs e) => NavigateNext();
    private void OnTodayButtonClick(object? sender, RoutedEventArgs e) => NavigateToday();
    private void OnWeekButtonClick(object? sender, RoutedEventArgs e) => ViewMode = CalendarViewMode.Week;
    private void OnMonthButtonClick(object? sender, RoutedEventArgs e) => ViewMode = CalendarViewMode.Month;
    private void OnMiniCalPreviousButtonClick(object? sender, RoutedEventArgs e) => MiniCalNavigate(-1);
    private void OnMiniCalNextButtonClick(object? sender, RoutedEventArgs e) => MiniCalNavigate(1);
    private void OnWeekViewTimeGridPointerPressed(object? sender, PointerPressedEventArgs e) => SelectedItem = null;

    private void Rebuild()
    {
        if (_headerTitle == null) return;

        _appointmentBorders.Clear();

        UpdateHeader();
        UpdateMiniCalendar();

        if (ViewMode == CalendarViewMode.Month)
            UpdateMonthView();
        else
            UpdateWeekView();
    }

    private void UpdateHeader()
    {
        if (_headerTitle == null) return;

        if (ViewMode == CalendarViewMode.Month)
        {
            _headerTitle.Text = DisplayDate.ToString("MMMM yyyy");
        }
        else
        {
            var weekStart = GetWeekStart(DisplayDate);
            var weekEnd = weekStart.AddDays(6);
            if (weekStart.Month == weekEnd.Month)
                _headerTitle.Text = $"{weekStart:MMMM d} – {weekEnd:d}, {weekEnd:yyyy}";
            else if (weekStart.Year == weekEnd.Year)
                _headerTitle.Text = $"{weekStart:MMM d} – {weekEnd:MMM d}, {weekEnd:yyyy}";
            else
                _headerTitle.Text = $"{weekStart:MMM d, yyyy} – {weekEnd:MMM d, yyyy}";
        }
    }

    private void UpdateMiniCalendar()
    {
        if (_miniCalGrid == null || _miniCalTitle == null) return;

        _miniCalTitle.Text = _miniCalDisplayMonth.ToString("MMMM yyyy");

        _miniCalGrid.Children.Clear();

        // Day-of-week headers
        var dayNames = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames;
        for (int i = 0; i < 7; i++)
        {
            int dayIndex = ((int)FirstDayOfWeek + i) % 7;
            var header = new TextBlock
            {
                Text = dayNames[dayIndex][..2],
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 11,
                Foreground = GetBrush("fxForegroundSecondaryBrush")
            };
            Grid.SetColumn(header, i);
            Grid.SetRow(header, 0);
            _miniCalGrid.Children.Add(header);
        }

        // Calculate days
        var firstOfMonth = new DateTimeOffset(_miniCalDisplayMonth.Year, _miniCalDisplayMonth.Month, 1, 0, 0, 0, _miniCalDisplayMonth.Offset);
        int startDayOffset = (((int)firstOfMonth.DayOfWeek - (int)FirstDayOfWeek) + 7) % 7;
        var gridStart = firstOfMonth.AddDays(-startDayOffset);

        var today = DateTimeOffset.Now.Date;

        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                var date = gridStart.AddDays(row * 7 + col);
                bool isCurrentMonth = date.Month == _miniCalDisplayMonth.Month && date.Year == _miniCalDisplayMonth.Year;
                bool isToday = date.Date == today;
                bool isSelected = SelectedDate.HasValue && date.Date == SelectedDate.Value.Date;

                var dayButton = new Button
                {
                    Content = date.Day.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Padding = new Thickness(0),
                    MinWidth = 0,
                    MinHeight = 0,
                    FontSize = 11,
                    Background = isToday ? GetBrush("fxCalendarTodayBrush")
                               : isSelected ? GetBrush("fxCalendarSelectedBrush")
                               : Brushes.Transparent,
                    Foreground = !isCurrentMonth ? GetBrush("fxCalendarOutOfMonthBrush")
                               : isToday ? GetBrush("fxAccentBrush")
                               : GetBrush("fxForegroundBrush"),
                    BorderThickness = new Thickness(0),
                    CornerRadius = new CornerRadius(4)
                };

                var capturedDate = date;
                dayButton.Click += (_, _) =>
                {
                    SelectedDate = capturedDate;
                    DisplayDate = capturedDate;
                };

                Grid.SetColumn(dayButton, col);
                Grid.SetRow(dayButton, row + 1);
                _miniCalGrid.Children.Add(dayButton);
            }
        }
    }

    private void UpdateMonthView()
    {
        if (_monthViewDayHeaders == null || _monthViewGrid == null) return;

        // Update day headers
        _monthViewDayHeaders.Children.Clear();
        var dayNames = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames;
        for (int i = 0; i < 7; i++)
        {
            int dayIndex = ((int)FirstDayOfWeek + i) % 7;
            var header = new TextBlock
            {
                Text = dayNames[dayIndex],
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                Foreground = GetBrush("fxForegroundSecondaryBrush"),
                Margin = new Thickness(0, 0, 0, 4)
            };
            Grid.SetColumn(header, i);
            _monthViewDayHeaders.Children.Add(header);
        }

        // Calculate grid days
        var firstOfMonth = new DateTimeOffset(DisplayDate.Year, DisplayDate.Month, 1, 0, 0, 0, DisplayDate.Offset);
        int startDayOffset = (((int)firstOfMonth.DayOfWeek - (int)FirstDayOfWeek) + 7) % 7;
        var gridStart = firstOfMonth.AddDays(-startDayOffset);

        var today = DateTimeOffset.Now.Date;
        var items = Items?.ToList() ?? new List<CalendarScheduleItem>();

        _monthViewGrid.Children.Clear();

        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                var date = gridStart.AddDays(row * 7 + col);
                bool isCurrentMonth = date.Month == DisplayDate.Month && date.Year == DisplayDate.Year;
                bool isToday = date.Date == today;
                bool isSelected = SelectedDate.HasValue && date.Date == SelectedDate.Value.Date;

                var cellContent = new StackPanel { Spacing = 1 };

                // Day number
                var dayNumber = new TextBlock
                {
                    Text = date.Day.ToString(),
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(4, 2),
                    Foreground = !isCurrentMonth ? GetBrush("fxCalendarOutOfMonthBrush")
                               : isToday ? GetBrush("fxAccentBrush")
                               : GetBrush("fxForegroundBrush"),
                    FontWeight = isToday ? FontWeight.Bold : FontWeight.Normal
                };
                cellContent.Children.Add(dayNumber);

                // Appointments for this day
                var dayItems = items.Where(item =>
                    item.Start.Date <= date.Date && item.End.Date >= date.Date)
                    .Take(3)
                    .ToList();

                foreach (var item in dayItems)
                {
                    var appointmentBorder = new Border
                    {
                        Background = item.Color ?? GetBrush("fxCalendarAppointmentBrush"),
                        CornerRadius = new CornerRadius(2),
                        Padding = new Thickness(3, 1),
                        Margin = new Thickness(2, 0),
                        Cursor = new Cursor(StandardCursorType.Hand)
                    };

                    var appointmentText = new TextBlock
                    {
                        Text = item.Title ?? "",
                        FontSize = 10,
                        Foreground = Brushes.White,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        MaxLines = 1
                    };
                    appointmentBorder.Child = appointmentText;

                    SetupAppointmentInteraction(appointmentBorder, item);

                    cellContent.Children.Add(appointmentBorder);
                }

                // More items indicator
                var totalDayItems = items.Count(item =>
                    item.Start.Date <= date.Date && item.End.Date >= date.Date);
                if (totalDayItems > 3)
                {
                    var moreText = new TextBlock
                    {
                        Text = $"+{totalDayItems - 3} more",
                        FontSize = 10,
                        Foreground = GetBrush("fxForegroundSecondaryBrush"),
                        Margin = new Thickness(4, 0)
                    };
                    cellContent.Children.Add(moreText);
                }

                var cellBorder = new Border
                {
                    BorderBrush = GetBrush("fxCalendarGridLineBrush"),
                    BorderThickness = new Thickness(0, 0, col < 6 ? 1 : 0, row < 5 ? 1 : 0),
                    Background = isToday ? GetBrush("fxCalendarTodayBrush")
                               : isSelected ? GetBrush("fxCalendarSelectedBrush")
                               : Brushes.Transparent,
                    Padding = new Thickness(2),
                    Child = cellContent
                };

                var capturedDate = date;
                cellBorder.PointerPressed += (_, _) =>
                {
                    SelectedDate = capturedDate;
                    SelectedItem = null;
                };

                Grid.SetColumn(cellBorder, col);
                Grid.SetRow(cellBorder, row);
                _monthViewGrid.Children.Add(cellBorder);
            }
        }
    }

    private void UpdateWeekView()
    {
        if (_weekViewDayHeaders == null || _weekViewTimeGrid == null) return;

        var weekStart = GetWeekStart(DisplayDate);
        var today = DateTimeOffset.Now.Date;
        var items = Items?.ToList() ?? new List<CalendarScheduleItem>();

        // Update day headers
        _weekViewDayHeaders.Children.Clear();

        // Empty cell above time column
        var emptyHeader = new TextBlock();
        Grid.SetColumn(emptyHeader, 0);
        _weekViewDayHeaders.Children.Add(emptyHeader);

        for (int i = 0; i < 7; i++)
        {
            var date = weekStart.AddDays(i);
            bool isToday = date.Date == today;

            var headerPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var dayName = new TextBlock
            {
                Text = date.ToString("ddd").ToUpperInvariant(),
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = isToday ? GetBrush("fxAccentBrush") : GetBrush("fxForegroundSecondaryBrush")
            };
            headerPanel.Children.Add(dayName);

            var dayNum = new TextBlock
            {
                Text = date.Day.ToString(),
                FontSize = 18,
                FontWeight = FontWeight.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = isToday ? GetBrush("fxAccentBrush") : GetBrush("fxForegroundBrush")
            };
            headerPanel.Children.Add(dayNum);

            Grid.SetColumn(headerPanel, i + 1);
            _weekViewDayHeaders.Children.Add(headerPanel);
        }

        // Build time grid
        _weekViewTimeGrid.Children.Clear();
        _weekViewTimeGrid.RowDefinitions.Clear();

        for (int hour = 0; hour < 24; hour++)
        {
            _weekViewTimeGrid.RowDefinitions.Add(new RowDefinition(HourHeight, GridUnitType.Pixel));
        }

        // Vertical day separators (full-height lines between columns)
        for (int col = 1; col < 7; col++)
        {
            var separator = new Border
            {
                Width = 1,
                Background = GetBrush("fxCalendarGridLineBrush"),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(separator, col);
            Grid.SetRow(separator, 0);
            Grid.SetRowSpan(separator, 24);
            _weekViewTimeGrid.Children.Add(separator);
        }

        // Time labels and hour lines
        for (int hour = 0; hour < 24; hour++)
        {
            // Time label
            var timeLabel = new TextBlock
            {
                Text = new TimeOnly(hour, 0).ToString("HH:mm"),
                FontSize = 11,
                Foreground = GetBrush("fxForegroundSecondaryBrush"),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(4, -6, 8, 0)
            };
            Grid.SetColumn(timeLabel, 0);
            Grid.SetRow(timeLabel, hour);
            _weekViewTimeGrid.Children.Add(timeLabel);

            // Horizontal line across day columns
            for (int col = 1; col <= 7; col++)
            {
                var line = new Border
                {
                    BorderBrush = GetBrush("fxCalendarGridLineBrush"),
                    BorderThickness = new Thickness(0, 1, 0, 0),
                    VerticalAlignment = VerticalAlignment.Top
                };
                Grid.SetColumn(line, col);
                Grid.SetRow(line, hour);
                _weekViewTimeGrid.Children.Add(line);
            }
        }

        // Place appointment items
        for (int dayIdx = 0; dayIdx < 7; dayIdx++)
        {
            var date = weekStart.AddDays(dayIdx);
            var dayItems = items.Where(item =>
                item.Start.Date <= date.Date && item.End.Date >= date.Date)
                .OrderBy(item => item.Start)
                .ToList();

            foreach (var item in dayItems)
            {
                // Calculate position
                var itemStart = item.Start.Date < date.Date
                    ? new TimeSpan(0, 0, 0)
                    : item.Start.TimeOfDay;
                var itemEnd = item.End.Date > date.Date
                    ? new TimeSpan(24, 0, 0)
                    : item.End.TimeOfDay;

                double topOffset = itemStart.TotalHours * HourHeight;
                double height = Math.Max((itemEnd - itemStart).TotalHours * HourHeight, 20);

                int startRow = (int)itemStart.TotalHours;
                if (startRow >= 24) startRow = 23;

                var appointmentBorder = new Border
                {
                    Background = item.Color ?? GetBrush("fxCalendarAppointmentBrush"),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 4),
                    Margin = new Thickness(2, topOffset - (startRow * HourHeight), 2, 0),
                    Height = height,
                    VerticalAlignment = VerticalAlignment.Top,
                    Cursor = new Cursor(StandardCursorType.Hand),
                    ClipToBounds = true
                };

                var textPanel = new StackPanel();
                var titleText = new TextBlock
                {
                    Text = item.Title ?? "",
                    FontSize = 11,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                textPanel.Children.Add(titleText);

                if (height > 36)
                {
                    var timeText = new TextBlock
                    {
                        Text = $"{item.Start:HH:mm} – {item.End:HH:mm}",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255))
                    };
                    textPanel.Children.Add(timeText);
                }

                appointmentBorder.Child = textPanel;

                SetupAppointmentInteraction(appointmentBorder, item);

                Grid.SetColumn(appointmentBorder, dayIdx + 1);
                Grid.SetRow(appointmentBorder, startRow);
                _weekViewTimeGrid.Children.Add(appointmentBorder);
            }

            // Current time indicator
            if (date.Date == today)
            {
                var now = DateTimeOffset.Now;
                double nowOffset = now.TimeOfDay.TotalHours * HourHeight;
                int nowRow = (int)now.TimeOfDay.TotalHours;
                if (nowRow >= 24) nowRow = 23;

                var timeLine = new Border
                {
                    Height = 2,
                    Background = GetBrush("fxCalendarCurrentTimeBrush"),
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, nowOffset - (nowRow * HourHeight), 0, 0),
                    ZIndex = 10
                };
                Grid.SetColumn(timeLine, dayIdx + 1);
                Grid.SetRow(timeLine, nowRow);
                _weekViewTimeGrid.Children.Add(timeLine);
            }
        }

        // Scroll to 8:00 AM on initial build
        if (!_initialScrollDone && _weekViewScrollViewer != null)
        {
            _initialScrollDone = true;
            _weekViewScrollViewer.Offset = new Vector(0, 8 * HourHeight);
        }
    }

    private void SetupAppointmentInteraction(Border border, CalendarScheduleItem item)
    {
        var baseBrush = border.Background ?? GetBrush("fxCalendarAppointmentBrush");
        bool isSelected = SelectedItem == item;

        if (isSelected)
            border.BorderBrush = Brushes.White;

        border.BorderThickness = new Thickness(isSelected ? 1.5 : 0);

        _appointmentBorders.Add((border, item, baseBrush));

        border.PointerEntered += (_, _) =>
        {
            border.Opacity = 0.8;
        };

        border.PointerExited += (_, _) =>
        {
            border.Opacity = 1.0;
        };

        border.PointerPressed += (_, e) =>
        {
            SelectedItem = item;
            e.Handled = true;
        };
    }

    private void UpdateAppointmentSelection()
    {
        foreach (var (border, item, _) in _appointmentBorders)
        {
            bool isSelected = SelectedItem == item;
            border.BorderBrush = isSelected ? Brushes.White : null;
            border.BorderThickness = new Thickness(isSelected ? 1.5 : 0);
        }
    }

    private DateTimeOffset GetWeekStart(DateTimeOffset date)
    {
        int diff = (((int)date.DayOfWeek - (int)FirstDayOfWeek) + 7) % 7;
        return date.AddDays(-diff).Date;
    }

    private IBrush GetBrush(string resourceKey)
    {
        if (this.TryFindResource(resourceKey, ActualThemeVariant, out var resource) && resource is IBrush brush)
            return brush;
        return Brushes.Gray;
    }
}
