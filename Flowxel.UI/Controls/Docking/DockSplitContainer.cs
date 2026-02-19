using Avalonia.Controls.Primitives;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia;

namespace Flowxel.UI.Controls.Docking;

public class DockSplitContainer : TemplatedControl
{
    private Grid? _grid;
    private ContentControl? _first;
    private GridSplitter? _splitter;
    private ContentControl? _second;

    public static readonly StyledProperty<Control?> FirstProperty =
        AvaloniaProperty.Register<DockSplitContainer, Control?>(nameof(First));

    public static readonly StyledProperty<Control?> SecondProperty =
        AvaloniaProperty.Register<DockSplitContainer, Control?>(nameof(Second));

    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<DockSplitContainer, Orientation>(nameof(Orientation), Orientation.Horizontal);

    public static readonly StyledProperty<GridLength> FirstSizeProperty =
        AvaloniaProperty.Register<DockSplitContainer, GridLength>(nameof(FirstSize), new GridLength(1, GridUnitType.Star));

    public static readonly StyledProperty<GridLength> SecondSizeProperty =
        AvaloniaProperty.Register<DockSplitContainer, GridLength>(nameof(SecondSize), new GridLength(1, GridUnitType.Star));

    public static readonly StyledProperty<bool> FirstResizableProperty =
        AvaloniaProperty.Register<DockSplitContainer, bool>(nameof(FirstResizable), true);

    public static readonly StyledProperty<bool> SecondResizableProperty =
        AvaloniaProperty.Register<DockSplitContainer, bool>(nameof(SecondResizable), true);

    public Control? First
    {
        get => GetValue(FirstProperty);
        set => SetValue(FirstProperty, value);
    }

    public Control? Second
    {
        get => GetValue(SecondProperty);
        set => SetValue(SecondProperty, value);
    }

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public GridLength FirstSize
    {
        get => GetValue(FirstSizeProperty);
        set => SetValue(FirstSizeProperty, value);
    }

    public GridLength SecondSize
    {
        get => GetValue(SecondSizeProperty);
        set => SetValue(SecondSizeProperty, value);
    }

    public bool FirstResizable
    {
        get => GetValue(FirstResizableProperty);
        set => SetValue(FirstResizableProperty, value);
    }

    public bool SecondResizable
    {
        get => GetValue(SecondResizableProperty);
        set => SetValue(SecondResizableProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _grid = e.NameScope.Find<Grid>("PART_Grid");
        _first = e.NameScope.Find<ContentControl>("PART_First");
        _splitter = e.NameScope.Find<GridSplitter>("PART_Splitter");
        _second = e.NameScope.Find<ContentControl>("PART_Second");

        ConfigureLayout();
        UpdatePseudoClasses();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == OrientationProperty)
        {
            ConfigureLayout();
            UpdatePseudoClasses();
        }
        else if (change.Property == FirstSizeProperty
                 || change.Property == SecondSizeProperty
                 || change.Property == FirstResizableProperty
                 || change.Property == SecondResizableProperty)
        {
            ConfigureLayout();
            UpdatePseudoClasses();
        }
    }

    private void ConfigureLayout()
    {
        if (_grid == null || _first == null || _splitter == null || _second == null)
            return;

        var showSplitter = FirstResizable && SecondResizable;

        _grid.ColumnDefinitions.Clear();
        _grid.RowDefinitions.Clear();

        if (Orientation == Orientation.Horizontal)
        {
            if (showSplitter)
            {
                _grid.ColumnDefinitions.Add(new ColumnDefinition(FirstSize));
                _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                _grid.ColumnDefinitions.Add(new ColumnDefinition(SecondSize));

                Grid.SetColumn(_first, 0);
                Grid.SetColumn(_splitter, 1);
                Grid.SetColumn(_second, 2);
            }
            else
            {
                _grid.ColumnDefinitions.Add(new ColumnDefinition(FirstSize));
                _grid.ColumnDefinitions.Add(new ColumnDefinition(SecondSize));

                Grid.SetColumn(_first, 0);
                Grid.SetColumn(_second, 1);
            }

            Grid.SetRow(_first, 0);
            Grid.SetRow(_splitter, 0);
            Grid.SetRow(_second, 0);

            // Reset rows
            Grid.SetRowSpan(_first, 1);
            Grid.SetRowSpan(_splitter, 1);
            Grid.SetRowSpan(_second, 1);
            Grid.SetColumnSpan(_first, 1);
            Grid.SetColumnSpan(_splitter, 1);
            Grid.SetColumnSpan(_second, 1);

            _splitter.ResizeDirection = GridResizeDirection.Columns;
            _splitter.Cursor = new Cursor(StandardCursorType.SizeWestEast);
            _splitter.Width = 5;
            _splitter.Height = double.NaN;
            _splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            _splitter.VerticalAlignment = VerticalAlignment.Stretch;
        }
        else
        {
            if (showSplitter)
            {
                _grid.RowDefinitions.Add(new RowDefinition(FirstSize));
                _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                _grid.RowDefinitions.Add(new RowDefinition(SecondSize));

                Grid.SetRow(_first, 0);
                Grid.SetRow(_splitter, 1);
                Grid.SetRow(_second, 2);
            }
            else
            {
                _grid.RowDefinitions.Add(new RowDefinition(FirstSize));
                _grid.RowDefinitions.Add(new RowDefinition(SecondSize));

                Grid.SetRow(_first, 0);
                Grid.SetRow(_second, 1);
            }

            Grid.SetColumn(_first, 0);
            Grid.SetColumn(_splitter, 0);
            Grid.SetColumn(_second, 0);

            // Reset spans
            Grid.SetRowSpan(_first, 1);
            Grid.SetRowSpan(_splitter, 1);
            Grid.SetRowSpan(_second, 1);
            Grid.SetColumnSpan(_first, 1);
            Grid.SetColumnSpan(_splitter, 1);
            Grid.SetColumnSpan(_second, 1);

            _splitter.ResizeDirection = GridResizeDirection.Rows;
            _splitter.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
            _splitter.Width = double.NaN;
            _splitter.Height = 5;
            _splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            _splitter.VerticalAlignment = VerticalAlignment.Stretch;
        }

        _splitter.IsVisible = showSplitter;
        _splitter.IsEnabled = showSplitter;
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":horizontal", Orientation == Orientation.Horizontal);
        PseudoClasses.Set(":vertical", Orientation == Orientation.Vertical);
        PseudoClasses.Set(":resizable", FirstResizable && SecondResizable);
    }
}
